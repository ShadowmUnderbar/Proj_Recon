using System;
using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// 1回の行動を 予備動作 → 攻撃 → 硬直 → 待機 の順に時間で進める。
    /// 秒数が0の段階は同じ呼び出しの中で通過する（全段階0なら命令した瞬間に攻撃して待機へ戻る）。
    /// </summary>
    public class BossActionPhaseMachine : IDisposable
    {
        private readonly ReactiveProperty<BossActionPhase> _phase = new(BossActionPhase.Ready);
        public ReadOnlyReactiveProperty<BossActionPhase> Phase => _phase;

        /// <summary>実行中（または直前に実行した）行動の番号</summary>
        public int ActionIndex { get; private set; }

        private float _windupTime;
        private float _activeTime;
        private float _recoveryTime;
        private float _elapsed;

        public void SetDurations(float windupTime, float activeTime, float recoveryTime)
        {
            _windupTime = Mathf.Max(0f, windupTime);
            _activeTime = Mathf.Max(0f, activeTime);
            _recoveryTime = Mathf.Max(0f, recoveryTime);
        }

        /// <summary>行動を始める。行動中なら何もせず false を返す</summary>
        public bool Begin(int actionIndex)
        {
            if (_phase.Value != BossActionPhase.Ready)
            {
                return false;
            }

            ActionIndex = actionIndex;
            _elapsed = 0f;
            _phase.Value = BossActionPhase.Windup;
            AdvanceElapsedPhases();
            return true;
        }

        /// <summary>経過時間を進める。止めたい間（ポーズ・スタン）は呼ばないこと</summary>
        public void Tick(float deltaTime)
        {
            if (_phase.Value == BossActionPhase.Ready)
            {
                return;
            }

            _elapsed += deltaTime;
            AdvanceElapsedPhases();
        }

        /// <summary>行動を打ち切って待機へ戻す（スタン・撃破）</summary>
        public void Cancel()
        {
            _elapsed = 0f;
            _phase.Value = BossActionPhase.Ready;
        }

        private void AdvanceElapsedPhases()
        {
            // 1フレームの経過が長くても、超えた分だけ段階を進める（超過分は次の段階へ繰り越す）
            while (_phase.Value != BossActionPhase.Ready && _elapsed >= GetDuration(_phase.Value))
            {
                _elapsed -= GetDuration(_phase.Value);
                _phase.Value = GetNext(_phase.Value);
            }
        }

        private float GetDuration(BossActionPhase phase)
        {
            return phase switch
            {
                BossActionPhase.Windup => _windupTime,
                BossActionPhase.Active => _activeTime,
                BossActionPhase.Recovery => _recoveryTime,
                _ => 0f
            };
        }

        private static BossActionPhase GetNext(BossActionPhase phase)
        {
            return phase switch
            {
                BossActionPhase.Windup => BossActionPhase.Active,
                BossActionPhase.Active => BossActionPhase.Recovery,
                _ => BossActionPhase.Ready
            };
        }

        public void Dispose()
        {
            _phase.Dispose();
        }
    }
}
