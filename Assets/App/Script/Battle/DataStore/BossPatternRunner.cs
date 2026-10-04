using System;
using System.Collections.Generic;
using App.Battle.Data;
using UnityEngine;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ボスグループ1組の行動台本を進める。
    /// 個体の状態（<see cref="BossMemberStatus"/>）を受け取り、行動・待機の命令を出力するだけで、
    /// 個体を直接操作しない（Unityのオブジェクトに依存しない）。
    /// </summary>
    public class BossPatternRunner
    {
        // 進めている台本（発狂フェイズへの切り替えで差し替わる）
        private IReadOnlyList<BossPatternStep> _steps;

        // 配置の振り分けに使う乱数（検証で結果を固定できるよう外から渡す）
        private readonly System.Random _random;

        // 配置を振り分けるときの作業用（生存している対象のメンバー番号・横へのずれ）
        private readonly List<int> _formationSlots = new();
        private readonly List<float> _formationOffsets = new();

        // メンバー番号（スロット）ごとの状態
        private readonly int[] _memberIds;
        private readonly BossMemberStatus[] _statuses;
        private readonly bool[] _isGone;
        private readonly bool[] _isHeld;

        private int _stepIndex;
        private float _waitElapsed;

        // 斜めの角を時計回りに並べたもの（左上 → 右上 → 右下 → 左下）。隣り合う角＝並びで1つ違い
        private static readonly BossFormationSlot[] DiagonalCorners =
        {
            BossFormationSlot.UpLeft, BossFormationSlot.UpRight, BossFormationSlot.DownRight, BossFormationSlot.DownLeft
        };

        // RandomLoop の残り回数（-1 は未開始。ループを抜けたら -1 に戻す）
        private int _loopRemaining = -1;

        // 現在の Act ステップで命令を出し終えたか（HoldOthers で対象の行動終了を待っている間 true）
        private bool _isCommanded;

        // TimeStopOrbit ステップの進み具合
        private readonly BossTimeStopOrbit _timeStopOrbit = new();

        public IReadOnlyList<int> MemberIds => _memberIds;

        /// <summary>メンバーが全員撃破・消去済みか</summary>
        public bool IsFinished { get; private set; }

        /// <summary>現在のステップ番号（検証・デバッグ表示用）</summary>
        public int StepIndex => _stepIndex;

        /// <summary>
        /// この台本が時を止めているか。全員いなくなると台本は進まず true のまま残るため、
        /// そのときは呼び出し側が時止めを解くこと（<see cref="SwitchPattern"/> は自分で解く命令を出す）
        /// </summary>
        public bool IsTimeStopping => _timeStopOrbit.IsTimeStopping;

        public BossPatternRunner(IReadOnlyList<int> memberIds, IReadOnlyList<BossPatternStep> steps,
            System.Random random)
        {
            _steps = steps;
            _random = random;
            _memberIds = new int[memberIds.Count];
            for (var i = 0; i < memberIds.Count; i++)
            {
                _memberIds[i] = memberIds[i];
            }

            _statuses = new BossMemberStatus[_memberIds.Length];
            _isGone = new bool[_memberIds.Length];
            _isHeld = new bool[_memberIds.Length];
            IsFinished = _memberIds.Length == 0;

            ValidateSteps();
        }

        /// <summary>個体の状態を更新する。このグループの個体でなければ false</summary>
        public bool TrySetStatus(int enemyId, BossMemberStatus status)
        {
            var slot = FindSlot(enemyId);
            if (slot < 0)
            {
                return false;
            }

            _statuses[slot] = status;
            return true;
        }

        /// <summary>個体を撃破・消去済みにする。このグループの個体でなければ false</summary>
        public bool TryMarkGone(int enemyId)
        {
            var slot = FindSlot(enemyId);
            if (slot < 0)
            {
                return false;
            }

            _isGone[slot] = true;
            _isHeld[slot] = false;
            IsFinished = Array.TrueForAll(_isGone, isGone => isGone);
            return true;
        }

        /// <summary>
        /// 台本を進め、この時点で出す命令を output に追加する。
        /// フリーズ・ウェーブ間ポーズ中は呼ばないこと（待ち時間が進んでしまう）。
        /// </summary>
        public void Tick(float deltaTime, List<BossDirectorCommand> output)
        {
            if (IsFinished || _steps.Count == 0)
            {
                return;
            }

            // 即座に終わるステップが続いても1ティックで台本を一周以上回さない（全員撃破済みの台本などで無限ループしないため）
            for (var processed = 0; processed < _steps.Count; processed++)
            {
                var result = ProcessStep(_steps[_stepIndex], ref deltaTime, output);

                if (result == StepResult.Blocked)
                {
                    return;
                }

                AdvanceStep();

                // 命令した直後は、個体の状態が反映されてから次のステップを判定する
                if (result == StepResult.CompletedAndYield)
                {
                    return;
                }
            }
        }

        private enum StepResult
        {
            Blocked,
            Completed,
            CompletedAndYield
        }

        private StepResult ProcessStep(BossPatternStep step, ref float deltaTime, List<BossDirectorCommand> output)
        {
            switch (step.Type)
            {
                case BossPatternStepType.Wait:
                    // 経過時間は1ティックに1回だけ使う（連続する Wait が同じ時間を二重に消費しないようにする）
                    _waitElapsed += deltaTime;
                    deltaTime = 0f;
                    return _waitElapsed >= step.WaitSeconds ? StepResult.Completed : StepResult.Blocked;

                case BossPatternStepType.WaitActionable:
                    return AreTargetsActionable(step) ? StepResult.Completed : StepResult.Blocked;

                case BossPatternStepType.Act:
                    return ProcessAct(step, output);

                case BossPatternStepType.RandomLoop:
                    return ProcessRandomLoop(step);

                case BossPatternStepType.DiagonalFormation:
                    if (!AreTargetsSpawned(step))
                    {
                        return StepResult.Blocked;
                    }

                    return AssignDiagonalFormation(step, output) ? StepResult.CompletedAndYield : StepResult.Completed;

                case BossPatternStepType.CrossFormation:
                    // 出現（プレハブの読み込み）が済んでいない個体には配置を届けられないため待つ
                    if (!AreTargetsSpawned(step))
                    {
                        return StepResult.Blocked;
                    }

                    // 配置し直した直後は、移動が反映されてから次のステップへ進む
                    return AssignCrossFormation(step, output) ? StepResult.CompletedAndYield : StepResult.Completed;

                case BossPatternStepType.TimeStopOrbit:
                    return ProcessTimeStopOrbit(step, ref deltaTime, output);

                default:
                    Debug.LogError($"[{nameof(BossPatternRunner)}] 未対応のステップ種別です: {step.Type}");
                    return StepResult.Completed;
            }
        }

        private StepResult ProcessAct(BossPatternStep step, List<BossDirectorCommand> output)
        {
            if (_isCommanded)
            {
                // HoldOthers: 対象が行動・硬直を終えるまで他のメンバーを待機させておく
                if (!AreTargetsActionable(step))
                {
                    return StepResult.Blocked;
                }

                ReleaseAllHeld(output);
                return StepResult.Completed;
            }

            if (!HasAliveTarget(step))
            {
                return StepResult.Completed;
            }

            // 全員が行動可能になるまで待ち、揃った時点で同じフレームに一斉に命令する
            if (!AreTargetsActionable(step))
            {
                return StepResult.Blocked;
            }

            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot))
                {
                    output.Add(BossDirectorCommand.Act(_memberIds[slot], step.ActionIndex));
                }
            }

            if (!step.HoldOthers)
            {
                return StepResult.CompletedAndYield;
            }

            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (_isGone[slot] || _isHeld[slot] || ContainsSlot(step, slot))
                {
                    continue;
                }

                _isHeld[slot] = true;
                output.Add(BossDirectorCommand.Hold(_memberIds[slot]));
            }

            _isCommanded = true;
            return StepResult.Blocked;
        }

        /// <summary>
        /// 時止めの回りこみ攻撃を1段階ずつ進める（命令を出したら次のティックで状態を見てから進む）。
        /// 時止め → 対象が90度回りこむ（1体は連射）→ 時止めを解く → 一息、の順。終わったら Completed
        /// </summary>
        private StepResult ProcessTimeStopOrbit(BossPatternStep step, ref float deltaTime, List<BossDirectorCommand> output)
        {
            var orbit = _timeStopOrbit;
            switch (orbit.CurrentStage)
            {
                case BossTimeStopOrbit.Stage.None:
                    if (!HasAliveTarget(step))
                    {
                        return StepResult.Completed;
                    }

                    // 対象以外も含めて全員の行動が明けてから止める（弾幕の途中で止めると、止めたあとに撃った弾が止まらない）
                    if (!AreAllAliveActionable())
                    {
                        return StepResult.Blocked;
                    }

                    BeginTimeStopOrbit(step, output);
                    return StepResult.Blocked;

                case BossTimeStopOrbit.Stage.Orbit:
                    if (!HasAliveTarget(step))
                    {
                        // 対象が全員いなくなった
                        FinishTimeStopOrbit(output);
                        return StepResult.Completed;
                    }

                    if (!AreTargetsActionable(step))
                    {
                        return StepResult.Blocked;
                    }

                    // 回りこみ終えたら時止めを解き、止めていた弾が動き出すのを一息の間その場で待つ
                    output.Add(BossDirectorCommand.EndTimeStop());
                    HoldAllAlive(output);
                    orbit.BeginBreath();
                    return StepResult.Blocked;

                default:
                    orbit.BreathElapsed += deltaTime;
                    deltaTime = 0f;
                    if (orbit.BreathElapsed < step.WaitSeconds)
                    {
                        return StepResult.Blocked;
                    }

                    FinishTimeStopOrbit(output);
                    return StepResult.Completed;
            }
        }

        /// <summary>
        /// 対象以外を待機させて時を止め、対象の全員に同じ向きで回りこませる（ランダムな1体だけ連射する行動にする）
        /// </summary>
        private void BeginTimeStopOrbit(BossPatternStep step, List<BossDirectorCommand> output)
        {
            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (_isGone[slot] || _isHeld[slot] || ContainsSlot(step, slot))
                {
                    continue;
                }

                _isHeld[slot] = true;
                output.Add(BossDirectorCommand.Hold(_memberIds[slot]));
            }

            output.Add(BossDirectorCommand.BeginTimeStop());

            // PickAliveTarget が生存している対象（重複なし）を _formationSlots に集める
            var shooterSlot = PickAliveTarget(step);
            var turnDirection = _random.Next(2) == 0 ? BossTurnDirection.Clockwise : BossTurnDirection.CounterClockwise;
            foreach (var slot in _formationSlots)
            {
                var actionIndex = slot == shooterSlot ? step.ActionIndex : step.PartnerActionIndex;
                output.Add(BossDirectorCommand.Act(_memberIds[slot], actionIndex, turnDirection));
            }

            _timeStopOrbit.BeginOrbit();
        }

        /// <summary>回りこみ攻撃を終える（時止め中なら解き、待機させていた個体を解放する）</summary>
        private void FinishTimeStopOrbit(List<BossDirectorCommand> output)
        {
            if (_timeStopOrbit.IsTimeStopping)
            {
                output.Add(BossDirectorCommand.EndTimeStop());
            }

            _timeStopOrbit.Reset();
            ReleaseAllHeld(output);
        }

        /// <summary>生存している対象から1体をランダムに選ぶ（いなければ -1）。生存している対象は _formationSlots に残る</summary>
        private int PickAliveTarget(BossPatternStep step)
        {
            _formationSlots.Clear();
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot) && !_formationSlots.Contains(slot))
                {
                    _formationSlots.Add(slot);
                }
            }

            return _formationSlots.Count == 0 ? -1 : _formationSlots[_random.Next(_formationSlots.Count)];
        }

        /// <summary>生存している全員（対象に限らない）が行動可能か</summary>
        private bool AreAllAliveActionable()
        {
            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (!_isGone[slot] && !_statuses[slot].IsActionable)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>生存している全員をその場で待機させる</summary>
        private void HoldAllAlive(List<BossDirectorCommand> output)
        {
            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (_isGone[slot] || _isHeld[slot])
                {
                    continue;
                }

                _isHeld[slot] = true;
                output.Add(BossDirectorCommand.Hold(_memberIds[slot]));
            }
        }

        /// <summary>
        /// 生存している対象を並べ替え、縦（上下）・横（左右）の順に交互に割り当てる。正負の側はそれぞれランダム。
        /// 割り当てた個体がいれば true
        /// </summary>
        private bool AssignCrossFormation(BossPatternStep step, List<BossDirectorCommand> output)
        {
            _formationSlots.Clear();
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot) && !_formationSlots.Contains(slot))
                {
                    _formationSlots.Add(slot);
                }
            }

            // どの個体が縦になるかを毎回変える（Fisher-Yates）
            for (var i = _formationSlots.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (_formationSlots[i], _formationSlots[j]) = (_formationSlots[j], _formationSlots[i]);
            }

            PickLateralOffsets(step, _formationSlots.Count);

            for (var i = 0; i < _formationSlots.Count; i++)
            {
                var isPositive = _random.Next(2) == 0;
                var formation = i % 2 == 0
                    ? (isPositive ? BossFormationSlot.Up : BossFormationSlot.Down)
                    : (isPositive ? BossFormationSlot.Right : BossFormationSlot.Left);
                output.Add(BossDirectorCommand.Formation(_memberIds[_formationSlots[i]], formation, _formationOffsets[i]));
            }

            return _formationSlots.Count > 0;
        }

        /// <summary>
        /// 初めて来たときに合計回数を決め、まだ残っていればループの先頭へ戻る（戻った先から進めるため Completed を返す）
        /// </summary>
        private StepResult ProcessRandomLoop(BossPatternStep step)
        {
            if (_loopRemaining < 0)
            {
                var min = Math.Max(1, step.LoopMin);
                var max = Math.Max(min, step.LoopMax);
                _loopRemaining = _random.Next(min, max + 1) - 1;
            }

            if (_loopRemaining == 0)
            {
                _loopRemaining = -1;
                return StepResult.Completed;
            }

            _loopRemaining--;
            // AdvanceStep で1つ進むので、その分を差し引いてループの先頭の1つ前に置く
            _stepIndex = Math.Max(0, _stepIndex - step.LoopBackSteps) - 1;
            return StepResult.Completed;
        }

        /// <summary>
        /// 生存している対象（先頭の2体）を、隣り合う斜めの角へ割り当てる。2体の向きが直交するので帯が×字になる。
        /// 割り当てた個体がいれば true
        /// </summary>
        private bool AssignDiagonalFormation(BossPatternStep step, List<BossDirectorCommand> output)
        {
            _formationSlots.Clear();
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot) && !_formationSlots.Contains(slot))
                {
                    _formationSlots.Add(slot);
                }
            }

            var first = _random.Next(DiagonalCorners.Length);
            var second = (first + (_random.Next(2) == 0 ? 1 : DiagonalCorners.Length - 1)) % DiagonalCorners.Length;

            for (var i = 0; i < _formationSlots.Count && i < 2; i++)
            {
                output.Add(BossDirectorCommand.Formation(_memberIds[_formationSlots[i]], DiagonalCorners[i == 0 ? first : second]));
            }

            return _formationSlots.Count > 0;
        }

        /// <summary>
        /// 個体ごとの横へのずれを候補からランダムに選ぶ（_formationOffsets に入れる）。
        /// RequireAlignedOne なら、全員が0以外になったときに1体をランダムに0へ置き換える
        /// </summary>
        private void PickLateralOffsets(BossPatternStep step, int count)
        {
            _formationOffsets.Clear();
            var candidates = step.LateralOffsets;
            if (candidates == null || candidates.Length == 0)
            {
                for (var i = 0; i < count; i++)
                {
                    _formationOffsets.Add(0f);
                }

                return;
            }

            var hasAligned = false;
            for (var i = 0; i < count; i++)
            {
                var offset = candidates[_random.Next(candidates.Length)];
                hasAligned |= offset == 0f;
                _formationOffsets.Add(offset);
            }

            if (step.RequireAlignedOne && !hasAligned && count > 0 && Array.IndexOf(candidates, 0f) >= 0)
            {
                _formationOffsets[_random.Next(count)] = 0f;
            }
        }

        /// <summary>
        /// 進める台本を差し替え、先頭から始める（発狂フェイズへの切り替え）。
        /// 行動中の個体は打ち切り、待機させていた個体は解放する
        /// </summary>
        public void SwitchPattern(IReadOnlyList<BossPatternStep> steps, List<BossDirectorCommand> output)
        {
            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (_isGone[slot])
                {
                    continue;
                }

                output.Add(BossDirectorCommand.Cancel(_memberIds[slot]));
            }

            // 時止めの回りこみ攻撃の途中なら時止めを解き、待機させていた個体を解放する
            FinishTimeStopOrbit(output);

            _steps = steps;
            _stepIndex = 0;
            _waitElapsed = 0f;
            _isCommanded = false;
            _loopRemaining = -1;
            ValidateSteps();
        }

        private void AdvanceStep()
        {
            _stepIndex = (_stepIndex + 1) % _steps.Count;
            _waitElapsed = 0f;
            _isCommanded = false;
        }

        private void ReleaseAllHeld(List<BossDirectorCommand> output)
        {
            for (var slot = 0; slot < _memberIds.Length; slot++)
            {
                if (!_isHeld[slot])
                {
                    continue;
                }

                _isHeld[slot] = false;
                output.Add(BossDirectorCommand.Release(_memberIds[slot]));
            }
        }

        /// <summary>生存している対象が全員行動可能か（生存者がいなければ true）</summary>
        private bool AreTargetsActionable(BossPatternStep step)
        {
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot) && !_statuses[slot].IsActionable)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>生存している対象が全員出現済みか（行動中・スタン中でもよい）</summary>
        private bool AreTargetsSpawned(BossPatternStep step)
        {
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot) && !_statuses[slot].IsSpawned)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasAliveTarget(BossPatternStep step)
        {
            foreach (var slot in step.MemberSlots)
            {
                if (IsAliveSlot(slot))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAliveSlot(int slot)
        {
            return slot >= 0 && slot < _memberIds.Length && !_isGone[slot];
        }

        private static bool ContainsSlot(BossPatternStep step, int slot)
        {
            return Array.IndexOf(step.MemberSlots, slot) >= 0;
        }

        private int FindSlot(int enemyId)
        {
            return Array.IndexOf(_memberIds, enemyId);
        }

        private void ValidateSteps()
        {
            for (var i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                if (step.Type == BossPatternStepType.RandomLoop)
                {
                    if (step.LoopBackSteps > i)
                    {
                        Debug.LogError(
                            $"[{nameof(BossPatternRunner)}] ステップ{i}の RandomLoop が台本の先頭より前（{step.LoopBackSteps}個前）へ戻ろうとしています。先頭から繰り返します");
                    }

                    continue;
                }

                if (step.Type == BossPatternStepType.Wait)
                {
                    continue;
                }

                foreach (var slot in step.MemberSlots)
                {
                    if (slot < 0 || slot >= _memberIds.Length)
                    {
                        Debug.LogError(
                            $"[{nameof(BossPatternRunner)}] ステップ{i}のメンバー番号 {slot} が範囲外です（メンバー数: {_memberIds.Length}）。この番号は無視します");
                    }
                }
            }
        }
    }
}
