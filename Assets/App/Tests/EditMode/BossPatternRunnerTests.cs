using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using App.Battle.Data;
using App.Battle.DataStore;
using App.Battle.Views.Enemy.AI.Boss;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace App.Tests.EditMode
{
    /// <summary>
    /// ボスの台本ランナー（BossPatternRunner）と行動段階（BossActionPhaseMachine）の単体テスト。
    /// 個体の状態を直接与え、出てくる命令を見る（Unity のシーンは使わない）。
    /// </summary>
    public class BossPatternRunnerTests
    {
        private const int A = 100;
        private const int B = 200;
        private const int C = 300;

        private static readonly BossMemberStatus Ready = new(true, BossActionPhase.Ready, false, false);
        private static readonly BossMemberStatus Busy = new(true, BossActionPhase.Recovery, false, false);
        private static readonly BossMemberStatus Windup = new(true, BossActionPhase.Windup, false, false);
        private static readonly BossMemberStatus Stun = new(true, BossActionPhase.Ready, true, false);

        // --- 台本のステップ（Inspector で組む値をリフレクションで入れる） ---

        private static BossPatternStep Step(BossPatternStepType type, int[] slots = null, int action = 0, bool hold = false,
            float wait = 0f, float[] offsets = null, bool aligned = false, int loopBack = 1, int loopMin = 1, int loopMax = 1,
            int memoryCount = 3, int replayAction = 0)
        {
            var step = new BossPatternStep();
            void Set(string field, object value) =>
                typeof(BossPatternStep).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(step, value);
            Set("_type", type);
            Set("_memberSlots", slots ?? Array.Empty<int>());
            Set("_actionIndex", action);
            Set("_holdOthers", hold);
            Set("_waitSeconds", wait);
            Set("_lateralOffsets", offsets ?? Array.Empty<float>());
            Set("_requireAlignedOne", aligned);
            Set("_loopBackSteps", loopBack);
            Set("_loopMin", loopMin);
            Set("_loopMax", loopMax);
            Set("_memoryCount", memoryCount);
            Set("_replayActionIndex", replayAction);
            return step;
        }

        private static BossPatternStep Act(int[] slots, int action = 0, bool hold = false) =>
            Step(BossPatternStepType.Act, slots, action, hold);

        private static BossPatternStep WaitActionable(params int[] slots) =>
            Step(BossPatternStepType.WaitActionable, slots);

        private static BossPatternStep Wait(float seconds) => Step(BossPatternStepType.Wait, wait: seconds);

        private static BossPatternStep Cross(float[] offsets = null, bool aligned = false) =>
            Step(BossPatternStepType.CrossFormation, new[] { 0, 1 }, offsets: offsets, aligned: aligned);

        // 時止めの記憶攻撃（予兆の行動 3・攻撃の行動 4・一息 1秒）
        private const int MemoryTelegraphAction = 3;
        private const int ReplayAction = 4;
        private const float Breath = 1f;

        private static BossPatternStep TimeStop(int[] slots = null, int count = 3, float[] offsets = null) =>
            Step(BossPatternStepType.TimeStopMemory, slots ?? new[] { 0, 1 }, MemoryTelegraphAction, wait: Breath,
                offsets: offsets, memoryCount: count, replayAction: ReplayAction);

        /// <summary>
        /// 命令に合わせて個体の状態を動かしながら台本を進め、出た命令を順に返す。
        /// 行動を命令された個体は次のティックで硬直中、その次のティックで行動可能に戻る
        /// </summary>
        private static List<BossDirectorCommand> Drive(BossPatternRunner runner, int ticks, float deltaTime = 0.1f,
            Func<BossDirectorCommand, bool> stopAfter = null)
        {
            var all = new List<BossDirectorCommand>();
            var busy = new HashSet<int>();
            for (var i = 0; i < ticks; i++)
            {
                foreach (var id in busy)
                {
                    runner.TrySetStatus(id, Ready);
                }

                busy.Clear();
                var output = Tick(runner, deltaTime);
                all.AddRange(output);
                foreach (var command in output.Where(c => c.Type == BossDirectorCommandType.Act))
                {
                    runner.TrySetStatus(command.EnemyId, Busy);
                    busy.Add(command.EnemyId);
                }

                if (stopAfter != null && output.Any(stopAfter))
                {
                    break;
                }
            }

            return all;
        }

        private static BossPatternRunner Runner(int[] ids, BossPatternStep[] steps, int seed = 1) =>
            new(ids, steps, new System.Random(seed));

        private static List<BossDirectorCommand> Tick(BossPatternRunner runner, float deltaTime = 0.016f)
        {
            var output = new List<BossDirectorCommand>();
            runner.Tick(deltaTime, output);
            return output;
        }

        private static void SetAll(BossPatternRunner runner, BossMemberStatus status, params int[] ids)
        {
            foreach (var id in ids)
            {
                runner.TrySetStatus(id, status);
            }
        }

        // --- 同時行動・相互排他・監視 ---

        [Test]
        public void Act_全員が行動可能になるまで待ち_同じティックで一斉に命令する()
        {
            var runner = Runner(new[] { A, B }, new[] { Act(new[] { 0, 1 }, 2) });
            Assert.That(Tick(runner), Is.Empty, "未出現（既定状態）のうちは命令しない");

            runner.TrySetStatus(A, Ready);
            runner.TrySetStatus(B, Busy);
            Assert.That(Tick(runner), Is.Empty, "B が硬直中なら A も待つ");

            runner.TrySetStatus(B, Ready);
            var output = Tick(runner);
            Assert.That(output.Count, Is.EqualTo(2));
            Assert.That(output.All(c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == 2), Is.True);
        }

        [Test]
        public void HoldOthers_攻撃中は他を待機させ_硬直明けで解除して次へ進む()
        {
            var runner = Runner(new[] { A, B }, new[] { Act(new[] { 0 }, 0, hold: true), Act(new[] { 1 }, 1) });
            SetAll(runner, Ready, A, B);

            var output = Tick(runner);
            Assert.That(output.Select(c => (c.Type, c.EnemyId)),
                Is.EqualTo(new[] { (BossDirectorCommandType.Act, A), (BossDirectorCommandType.Hold, B) }));

            runner.TrySetStatus(A, Windup);
            Assert.That(Tick(runner), Is.Empty, "A の予備動作中");
            runner.TrySetStatus(A, Busy);
            Assert.That(Tick(runner), Is.Empty, "A の硬直中も B は待機のまま");

            runner.TrySetStatus(A, Ready);
            output = Tick(runner);
            Assert.That(output.Select(c => (c.Type, c.EnemyId)),
                Is.EqualTo(new[] { (BossDirectorCommandType.Release, B), (BossDirectorCommandType.Act, B) }));
        }

        [Test]
        public void WaitActionable_硬直やスタンが明けるまで次の行動を始めない()
        {
            var runner = Runner(new[] { A, B }, new[] { Act(new[] { 0 }), WaitActionable(0), Act(new[] { 1 }) });
            SetAll(runner, Ready, A, B);
            Assert.That(Tick(runner).Count, Is.EqualTo(1), "Act(A) の直後はそのティックで止まる");

            runner.TrySetStatus(A, Busy);
            Assert.That(Tick(runner), Is.Empty, "A の硬直中は B を待つ");
            runner.TrySetStatus(A, Stun);
            Assert.That(Tick(runner), Is.Empty, "A のスタン中も B を待つ");

            runner.TrySetStatus(A, Ready);
            var output = Tick(runner);
            Assert.That(output.Count, Is.EqualTo(1));
            Assert.That(output[0].EnemyId, Is.EqualTo(B));
        }

        [Test]
        public void 撃破_待機中の相手を解放し_対象が全員いないステップは飛ばす()
        {
            var runner = Runner(new[] { A, B, C }, new[] { Act(new[] { 0 }, 0, hold: true), Act(new[] { 1, 2 }, 1) });
            SetAll(runner, Ready, A, B, C);
            Assert.That(Tick(runner).Count(c => c.Type == BossDirectorCommandType.Hold), Is.EqualTo(2));

            runner.TrySetStatus(A, Busy);
            runner.TryMarkGone(A);
            var output = Tick(runner);
            Assert.That(output.Count(c => c.Type == BossDirectorCommandType.Release), Is.EqualTo(2), "A 撃破で B・C を解放");
            Assert.That(output.Count(c => c.Type == BossDirectorCommandType.Act), Is.EqualTo(2), "続けて B・C が同時に行動");

            output = Tick(runner);
            Assert.That(output.All(c => c.Type == BossDirectorCommandType.Act && c.EnemyId != A), Is.True, "A のステップを飛ばす");

            runner.TryMarkGone(B);
            runner.TryMarkGone(C);
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(Tick(runner), Is.Empty);
        }

        [Test]
        public void Wait_経過時間で進み_同じティックで二重に消費しない()
        {
            var runner = Runner(new[] { A }, new[] { Wait(0.5f), Wait(0.5f), Act(new[] { 0 }) });
            runner.TrySetStatus(A, Ready);

            Assert.That(Tick(runner, 0.3f), Is.Empty);
            Assert.That(runner.StepIndex, Is.EqualTo(0));
            Assert.That(Tick(runner, 0.3f), Is.Empty);
            Assert.That(runner.StepIndex, Is.EqualTo(1), "残り時間は次の Wait へ持ち越さない");
            Assert.That(Tick(runner, 0.5f).Count, Is.EqualTo(1));
        }

        [Test]
        public void 即時に終わるステップだけの台本でもTickが返り_範囲外のメンバー番号はエラーにする()
        {
            var runner = Runner(new[] { A }, new[] { Wait(0f), WaitActionable(0) });
            runner.TrySetStatus(A, Ready);
            Tick(runner);

            LogAssert.Expect(LogType.Error, new Regex("範囲外"));
            var invalid = Runner(new[] { A }, new[] { Act(new[] { 5 }) });
            invalid.TrySetStatus(A, Ready);
            Assert.That(Tick(invalid), Is.Empty, "範囲外のメンバーだけの Act は飛ばす");
        }

        // --- 配置 ---

        [Test]
        public void CrossFormation_必ず縦と横の組で_どちらが縦か正負の側は偏らない()
        {
            var random = new System.Random(42);
            var verticalA = 0;
            var seen = new HashSet<BossFormationSlot>();
            for (var trial = 0; trial < 400; trial++)
            {
                var runner = new BossPatternRunner(new[] { A, B }, new[] { Cross() }, random);
                SetAll(runner, Ready, A, B);
                var output = Tick(runner);
                var a = output.First(c => c.EnemyId == A).Slot;
                var b = output.First(c => c.EnemyId == B).Slot;
                Assert.That(a.IsVertical(), Is.Not.EqualTo(b.IsVertical()), "縦と横の組");
                if (a.IsVertical()) verticalA++;
                seen.Add(a);
                seen.Add(b);
            }

            Assert.That(verticalA, Is.InRange(100, 300), "どちらが縦になるかは偏らない");
            Assert.That(seen, Is.EquivalentTo(new[] { BossFormationSlot.Up, BossFormationSlot.Down, BossFormationSlot.Left, BossFormationSlot.Right }));
        }

        [Test]
        public void CrossFormation_出現前は配置せず_配置した直後のティックでは次へ進まない()
        {
            var runner = Runner(new[] { A, B }, new[] { Cross(), Act(new[] { 0 }) });
            Assert.That(Tick(runner), Is.Empty, "出現前の個体がいるうちは配置しない");

            SetAll(runner, Ready, A, B);
            Assert.That(Tick(runner).All(c => c.Type == BossDirectorCommandType.Formation), Is.True);
            Assert.That(Tick(runner).Count, Is.EqualTo(1), "次のティックで Act");
        }

        [Test]
        public void CrossFormation_1体だけ残ったら残った個体に配置する()
        {
            var runner = Runner(new[] { A, B }, new[] { Cross() }, seed: 5);
            runner.TrySetStatus(A, Stun);
            runner.TryMarkGone(B);
            var output = Tick(runner);
            Assert.That(output.Count, Is.EqualTo(1));
            Assert.That(output[0].EnemyId, Is.EqualTo(A));
            Assert.That(output[0].Slot.IsVertical(), Is.True);
        }

        [Test]
        public void CrossFormation_横のずれは候補から選び_RequireAlignedOneなら必ず1体は0()
        {
            var random = new System.Random(7);
            var offsets = new[] { -5f, 0f, 5f };
            var seen = new HashSet<float>();
            var bothShiftedWithoutRule = 0;
            for (var trial = 0; trial < 400; trial++)
            {
                var runner = new BossPatternRunner(new[] { A, B }, new[] { Cross(offsets, aligned: true) }, random);
                SetAll(runner, Ready, A, B);
                var output = Tick(runner);
                Assert.That(output.Any(c => c.LateralOffset == 0f), Is.True, "少なくとも1体は重なる");
                foreach (var c in output) seen.Add(c.LateralOffset);

                var free = new BossPatternRunner(new[] { A, B }, new[] { Cross(offsets) }, random);
                SetAll(free, Ready, A, B);
                if (Tick(free).All(c => c.LateralOffset != 0f)) bothShiftedWithoutRule++;
            }

            Assert.That(seen, Is.EquivalentTo(offsets));
            Assert.That(bothShiftedWithoutRule, Is.GreaterThan(50), "指定なしなら両方ずれる回もある（指定が効いていることの確認）");
        }

        [Test]
        public void DiagonalFormation_2体を隣り合う斜めの角へ置き_向きが直交する()
        {
            var random = new System.Random(12);
            var ring = new[] { BossFormationSlot.UpLeft, BossFormationSlot.UpRight, BossFormationSlot.DownRight, BossFormationSlot.DownLeft };
            var pairs = new HashSet<string>();
            for (var trial = 0; trial < 400; trial++)
            {
                var runner = new BossPatternRunner(new[] { A, B }, new[] { Step(BossPatternStepType.DiagonalFormation, new[] { 0, 1 }) }, random);
                SetAll(runner, Ready, A, B);
                var output = Tick(runner);
                var a = output.First(c => c.EnemyId == A).Slot;
                var b = output.First(c => c.EnemyId == B).Slot;
                var diff = (Array.IndexOf(ring, a) - Array.IndexOf(ring, b) + 4) % 4;
                Assert.That(diff == 1 || diff == 3, Is.True, $"隣り合う角（{a} と {b}）");
                Assert.That(Vector3.Dot(a.ToDirection(), b.ToDirection()), Is.EqualTo(0f).Within(1e-4f), "向きが直交する");
                pairs.Add($"{Mathf.Min(Array.IndexOf(ring, a), Array.IndexOf(ring, b))}-{Mathf.Max(Array.IndexOf(ring, a), Array.IndexOf(ring, b))}");
            }

            Assert.That(pairs.Count, Is.EqualTo(4), "4通りの角の組がすべて出る");
        }

        // --- 繰り返し・台本の差し替え ---

        [Test]
        public void RandomLoop_直前のステップを合計Min以上Max以下の回数だけ繰り返す()
        {
            var random = new System.Random(11);
            var counts = new HashSet<int>();
            for (var trial = 0; trial < 400; trial++)
            {
                // Act(行動1) → RandomLoop(直前1つを3〜6回) → Act(行動9)
                var runner = new BossPatternRunner(new[] { A },
                    new[] { Act(new[] { 0 }, 1), Step(BossPatternStepType.RandomLoop, loopBack: 1, loopMin: 3, loopMax: 6), Act(new[] { 0 }, 9) },
                    random);
                runner.TrySetStatus(A, Ready);
                var repeats = 0;
                for (var tick = 0; tick < 20; tick++)
                {
                    var output = Tick(runner);
                    if (output.Any(c => c.ActionIndex == 9)) break;
                    repeats += output.Count(c => c.ActionIndex == 1);
                }

                Assert.That(repeats, Is.InRange(3, 6));
                counts.Add(repeats);
            }

            Assert.That(counts, Is.EquivalentTo(new[] { 3, 4, 5, 6 }), "3〜6回がすべて出る");
        }

        [Test]
        public void SwitchPattern_行動を打ち切り待機を解いて新しい台本の先頭から始める()
        {
            var runner = Runner(new[] { A, B }, new[] { Act(new[] { 0 }, 0, hold: true) });
            SetAll(runner, Ready, A, B);
            Tick(runner);
            runner.TrySetStatus(A, Busy);

            var output = new List<BossDirectorCommand>();
            runner.SwitchPattern(new[] { WaitActionable(0, 1), Act(new[] { 0, 1 }, 1) }, output);
            Assert.That(output.Count(c => c.Type == BossDirectorCommandType.Cancel), Is.EqualTo(2));
            Assert.That(output.Any(c => c.Type == BossDirectorCommandType.Release && c.EnemyId == B), Is.True);
            Assert.That(runner.StepIndex, Is.EqualTo(0));

            Assert.That(Tick(runner), Is.Empty, "打ち切りが反映されるまで待つ");
            runner.TrySetStatus(A, Ready);
            output = Tick(runner);
            Assert.That(output.Count, Is.EqualTo(2));
            Assert.That(output.All(c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == 1), Is.True);
        }

        // --- 時止めの記憶攻撃 ---

        [Test]
        public void TimeStopMemory_時を止めて1体ずつ予兆を見せ_解いて一息おき_同じ個体と配置で同じ順に攻撃する()
        {
            var runner = Runner(new[] { A, B }, new[] { TimeStop(offsets: new[] { -5f, 0f, 5f }), Act(new[] { 0, 1 }, 9) });
            SetAll(runner, Ready, A, B);

            var commands = Drive(runner, 200, stopAfter: c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == 9);
            var types = commands.Select(c => c.Type).ToList();

            // 時止めの前に全員を待機させる
            var begin = types.IndexOf(BossDirectorCommandType.BeginTimeStop);
            Assert.That(begin, Is.GreaterThanOrEqualTo(0), "時止めを始める");
            Assert.That(commands.Take(begin).Where(c => c.Type == BossDirectorCommandType.Hold).Select(c => c.EnemyId),
                Is.EquivalentTo(new[] { A, B }), "時止めの前に2体とも待機させる");

            var end = types.IndexOf(BossDirectorCommandType.EndTimeStop);
            Assert.That(end, Is.GreaterThan(begin), "予兆を見せ終えたら時止めを解く");
            Assert.That(types.Count(t => t == BossDirectorCommandType.EndTimeStop), Is.EqualTo(1));

            // 時止めの間: 配置 → 予兆の行動 を1体ずつ3回
            var during = commands.Skip(begin + 1).Take(end - begin - 1).ToList();
            var shownFormations = during.Where(c => c.Type == BossDirectorCommandType.Formation).ToList();
            var shownActs = during.Where(c => c.Type == BossDirectorCommandType.Act).ToList();
            Assert.That(shownFormations.Count, Is.EqualTo(3), "予兆の配置は3回");
            Assert.That(shownActs.Select(c => c.ActionIndex), Is.All.EqualTo(MemoryTelegraphAction), "時止め中は予兆だけの行動");
            Assert.That(shownActs.Select(c => c.EnemyId), Is.EqualTo(shownFormations.Select(c => c.EnemyId)),
                "配置した個体が、その配置で予兆を出す（1回に1体）");
            var axisSlots = new[] { BossFormationSlot.Up, BossFormationSlot.Down, BossFormationSlot.Left, BossFormationSlot.Right };
            Assert.That(shownFormations.All(c => axisSlots.Contains(c.Slot)), Is.True, "配置は上下左右のどれか");
            Assert.That(shownFormations.All(c => new[] { -5f, 0f, 5f }.Contains(c.LateralOffset)), Is.True, "横のずれは候補から選ぶ");

            // 時止めを解いたあと: 同じ個体・同じ配置・同じ順で攻撃
            var after = commands.Skip(end + 1).ToList();
            var replayFormations = after.Where(c => c.Type == BossDirectorCommandType.Formation).ToList();
            var replayActs = after.Where(c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == ReplayAction).ToList();
            Assert.That(replayFormations.Select(c => (c.EnemyId, c.Slot, c.LateralOffset)),
                Is.EqualTo(shownFormations.Select(c => (c.EnemyId, c.Slot, c.LateralOffset))), "見せたときと同じ個体・配置・順");
            Assert.That(replayActs.Select(c => c.EnemyId), Is.EqualTo(shownActs.Select(c => c.EnemyId)), "見せた順に攻撃する");

            // 攻撃が済んだら待機を解いて次のステップへ
            var lastReplay = after.FindLastIndex(c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == ReplayAction);
            Assert.That(after.Skip(lastReplay + 1).Where(c => c.Type == BossDirectorCommandType.Release).Select(c => c.EnemyId),
                Is.EquivalentTo(new[] { A, B }), "終わったら2体とも待機を解く");
            Assert.That(after.Last().ActionIndex, Is.EqualTo(9), "次のステップへ進む");
            Assert.That(runner.IsTimeStopping, Is.False);
        }

        [Test]
        public void TimeStopMemory_時止めを解いてから一息の秒数が経つまで攻撃を始めない()
        {
            var runner = Runner(new[] { A, B }, new[] { TimeStop(count: 1) });
            SetAll(runner, Ready, A, B);

            Drive(runner, 50, stopAfter: c => c.Type == BossDirectorCommandType.EndTimeStop);
            Assert.That(runner.IsTimeStopping, Is.False);

            // 1秒の一息を 0.25秒ずつ進める（解いたティックでは時間を使わない）
            Assert.That(Drive(runner, 3, 0.25f), Is.Empty, "0.75秒では攻撃を始めない");
            var output = Drive(runner, 3, 0.25f);
            Assert.That(output.Any(c => c.Type == BossDirectorCommandType.Formation), Is.True, "1秒経ったら攻撃の配置を出す");
        }

        [Test]
        public void TimeStopMemory_対象以外も含めて全員の行動が明けるまで時を止めない()
        {
            var runner = Runner(new[] { A, B }, new[] { TimeStop(slots: new[] { 0 }) });
            runner.TrySetStatus(A, Ready);
            runner.TrySetStatus(B, Busy);
            Assert.That(Tick(runner), Is.Empty, "対象外の B が弾幕などの最中なら待つ");

            runner.TrySetStatus(B, Ready);
            var output = Tick(runner);
            Assert.That(output.Select(c => c.Type), Does.Contain(BossDirectorCommandType.BeginTimeStop));
            Assert.That(runner.IsTimeStopping, Is.True);

            var commands = Drive(runner, 100, stopAfter: c => c.Type == BossDirectorCommandType.EndTimeStop);
            Assert.That(commands.Where(c => c.Type == BossDirectorCommandType.Formation).Select(c => c.EnemyId),
                Is.All.EqualTo(A), "予兆を見せるのは対象の A だけ");
        }

        [Test]
        public void TimeStopMemory_途中で発狂フェイズへ切り替えると時止めを解いて待機も解く()
        {
            var runner = Runner(new[] { A, B }, new[] { TimeStop() });
            SetAll(runner, Ready, A, B);
            Drive(runner, 3);
            Assert.That(runner.IsTimeStopping, Is.True);

            var output = new List<BossDirectorCommand>();
            runner.SwitchPattern(new[] { Act(new[] { 0 }, 1) }, output);
            Assert.That(output.Select(c => c.Type), Does.Contain(BossDirectorCommandType.EndTimeStop));
            Assert.That(output.Where(c => c.Type == BossDirectorCommandType.Release).Select(c => c.EnemyId),
                Is.EquivalentTo(new[] { A, B }));
            Assert.That(runner.IsTimeStopping, Is.False);
        }

        [Test]
        public void TimeStopMemory_撃破された個体の攻撃は飛ばす()
        {
            // 1回目の予兆を見せた個体を、時止めが解けたあとで撃破する
            var runner = Runner(new[] { A, B }, new[] { TimeStop(), Act(new[] { 0, 1 }, 9) });
            SetAll(runner, Ready, A, B);
            var shown = Drive(runner, 200, stopAfter: c => c.Type == BossDirectorCommandType.EndTimeStop)
                .Where(c => c.Type == BossDirectorCommandType.Act).Select(c => c.EnemyId).ToList();
            var dead = shown[0];
            runner.TryMarkGone(dead);

            var after = Drive(runner, 200, stopAfter: c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == 9);
            Assert.That(after.Where(c => c.Type == BossDirectorCommandType.Act && c.ActionIndex == ReplayAction).Select(c => c.EnemyId),
                Is.EqualTo(shown.Where(id => id != dead)), "撃破された個体の分だけ飛ばし、残りは同じ順");
            Assert.That(after.Any(c => c.EnemyId == dead), Is.False, "撃破された個体へは命令しない");
        }

        [Test]
        public void TimeStopMemory_時止めの間に全員撃破されたら台本は時止め中のまま終わる()
        {
            // 全員いなくなった台本は進まないため、時止めは BossGroupUseCase が撃破・消去の通知で解く（BossGroupDataStore.IsTimeStopping は終わったグループを数えない）
            var runner = Runner(new[] { A, B }, new[] { TimeStop() });
            SetAll(runner, Ready, A, B);
            Drive(runner, 3);
            runner.TryMarkGone(A);
            runner.TryMarkGone(B);
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(runner.IsTimeStopping, Is.True);
        }

        // --- 行動段階 ---

        [Test]
        public void BossActionPhaseMachine_予備動作から攻撃_硬直_待機の順に時間で進む()
        {
            using var machine = new BossActionPhaseMachine();
            var log = new List<BossActionPhase>();
            using var subscription = machine.Phase.Subscribe(log.Add);

            machine.SetDurations(0.5f, 0.2f, 1f);
            Assert.That(machine.Begin(3), Is.True);
            Assert.That(machine.Phase.CurrentValue, Is.EqualTo(BossActionPhase.Windup));
            Assert.That(machine.ActionIndex, Is.EqualTo(3));
            Assert.That(machine.Begin(4), Is.False, "行動中は受け付けない");

            machine.Tick(0.6f);
            Assert.That(machine.Phase.CurrentValue, Is.EqualTo(BossActionPhase.Active));
            machine.Tick(0.2f);
            Assert.That(machine.Phase.CurrentValue, Is.EqualTo(BossActionPhase.Recovery), "超過分を次の段階へ持ち越す");
            machine.Tick(0.95f);
            Assert.That(machine.Phase.CurrentValue, Is.EqualTo(BossActionPhase.Ready));

            machine.SetDurations(0f, 0f, 0f);
            log.Clear();
            machine.Begin(0);
            Assert.That(log, Is.EqualTo(new[] { BossActionPhase.Windup, BossActionPhase.Active, BossActionPhase.Recovery, BossActionPhase.Ready }),
                "全段階0なら Begin の中で一巡する");

            machine.SetDurations(1f, 1f, 1f);
            machine.Begin(0);
            machine.Cancel();
            Assert.That(machine.Phase.CurrentValue, Is.EqualTo(BossActionPhase.Ready));
        }
    }
}
