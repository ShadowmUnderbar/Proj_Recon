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
        private readonly IReadOnlyList<BossPatternStep> _steps;

        // メンバー番号（スロット）ごとの状態
        private readonly int[] _memberIds;
        private readonly BossMemberStatus[] _statuses;
        private readonly bool[] _isGone;
        private readonly bool[] _isHeld;

        private int _stepIndex;
        private float _waitElapsed;

        // 現在の Act ステップで命令を出し終えたか（HoldOthers で対象の行動終了を待っている間 true）
        private bool _isCommanded;

        public IReadOnlyList<int> MemberIds => _memberIds;

        /// <summary>メンバーが全員撃破・消去済みか</summary>
        public bool IsFinished { get; private set; }

        /// <summary>現在のステップ番号（検証・デバッグ表示用）</summary>
        public int StepIndex => _stepIndex;

        public BossPatternRunner(IReadOnlyList<int> memberIds, IReadOnlyList<BossPatternStep> steps)
        {
            _steps = steps;
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
