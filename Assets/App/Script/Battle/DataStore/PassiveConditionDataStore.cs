using System;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// パッシブ効果の発動条件が現在成立しているかを判定する。
    /// 状態ベース（HP割合）とタイマー型（回避直後N秒）の両方を吸収する。
    /// </summary>
    public class PassiveConditionDataStore : IPassiveConditionDataStore, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly CompositeDisposable _disposables = new();

        // 直近で回避した時刻（Time.time 基準）。未回避時は判定が成立しないよう負の無限大で初期化。
        private float _dodgeTriggeredAt = float.NegativeInfinity;

        [Inject]
        public PassiveConditionDataStore(
            IPlayerStateDataStore playerStateDataStore,
            IGameInputDataStore gameInputDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;

            // 回避入力が立ち上がった瞬間に発動時刻を記録（タイマー型条件のトリガー）
            gameInputDataStore.IsDodge
                .Where(isDodge => isDodge)
                .Subscribe(_ => _dodgeTriggeredAt = Time.time)
                .AddTo(_disposables);
        }

        public bool IsSatisfied(ConditionType conditionType, float conditionValue)
        {
            switch (conditionType)
            {
                case ConditionType.None:
                    return true;

                case ConditionType.HpBelow:
                    return GetHpRatio() < conditionValue;

                case ConditionType.HpAbove:
                    return GetHpRatio() >= conditionValue;

                case ConditionType.AfterDodge:
                    // 回避直後 conditionValue 秒間だけ成立
                    return Time.time < _dodgeTriggeredAt + conditionValue;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 現在HPの割合（0〜1）を返す。最大HPが0以下のときは0として扱う。
        /// </summary>
        private float GetHpRatio()
        {
            var maxHealth = _playerStateDataStore.MaxHealth.Value;
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return _playerStateDataStore.Health.Value / maxHealth;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
