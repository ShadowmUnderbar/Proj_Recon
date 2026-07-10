using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 取得済みバフの実行時状態（発動条件の進行・残り効果時間）を管理する
    /// </summary>
    public class BuffStateDataStore : IBuffStateDataStore, ITickable
    {
        private class BuffState
        {
            public BuffMasterData Master;

            // HitCount条件: 次の発動に向けたヒット蓄積数
            public int HitCount;

            // HitCount条件: 残り効果時間（秒）。0以下なら非アクティブ
            public float RemainingTime;

            // HealthRatioBelow条件: 条件成立中フラグ
            public bool IsConditionActive;

            public bool IsActive => Master.ConditionType switch
            {
                BuffConditionType.HitCount => RemainingTime > 0f,
                BuffConditionType.HealthRatioBelow => IsConditionActive,
                _ => false
            };
        }

        private readonly List<BuffState> _buffStates = new();

        private float _currentHealthRatio = 1f;

        public void Tick()
        {
            foreach (var state in _buffStates)
            {
                if (state.RemainingTime > 0f)
                {
                    state.RemainingTime -= Time.deltaTime;
                }
            }
        }

        public void AddBuff(BuffMasterData buffData)
        {
            if (_buffStates.Any(state => state.Master.Id == buffData.Id))
            {
                return;
            }

            var state = new BuffState { Master = buffData };

            // 取得時点のHP割合で条件を初期判定する
            if (buffData.ConditionType == BuffConditionType.HealthRatioBelow)
            {
                state.IsConditionActive = _currentHealthRatio <= buffData.ConditionValue;
            }

            _buffStates.Add(state);
        }

        public void NotifyHit()
        {
            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.HitCount)
                {
                    continue;
                }

                state.HitCount++;
                if (state.HitCount < state.Master.ConditionValue)
                {
                    continue;
                }

                // 発動: カウンタをリセットし、効果時間をリフレッシュ（スタックはしない）
                state.HitCount = 0;
                state.RemainingTime = state.Master.Duration;
            }
        }

        public void SetHealthRatio(float healthRatio)
        {
            _currentHealthRatio = healthRatio;

            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.HealthRatioBelow)
                {
                    continue;
                }

                state.IsConditionActive = healthRatio <= state.Master.ConditionValue;
            }
        }

        public float CalcMultiply(BuffEffectType effectType)
        {
            var result = 1f;
            foreach (var state in _buffStates)
            {
                if (state.Master.EffectType != effectType || !state.IsActive)
                {
                    continue;
                }

                result *= state.Master.EffectValue;
            }

            return result;
        }

        public void Reset()
        {
            _buffStates.Clear();
            _currentHealthRatio = 1f;
        }
    }
}
