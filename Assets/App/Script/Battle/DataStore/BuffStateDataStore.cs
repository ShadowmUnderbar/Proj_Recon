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

            // HpBelow条件: 条件成立中フラグ
            public bool IsConditionActive;

            // HitDifferentEnemy条件: 現在の累積効果倍率（初期値1=補正なし。上限は Master.EffectValue）
            public float StackMultiplier = 1f;

            // HitDifferentEnemy条件: 直前に命中した敵ID（-1=未命中）。同一敵の連続命中でリセット判定に使う
            public int LastHitEnemyId = -1;

            public bool IsActive => Master.ConditionType switch
            {
                BuffConditionType.HitCount => RemainingTime > 0f,
                BuffConditionType.HpBelow => IsConditionActive,
                BuffConditionType.HitDifferentEnemy => StackMultiplier > 1f,
                BuffConditionType.OnDamaged => RemainingTime > 0f,
                _ => false
            };

            // アクティブ時に適用する効果倍率。スタック型は累積倍率、それ以外はマスターの固定倍率
            public float EffectMultiplier => Master.ConditionType == BuffConditionType.HitDifferentEnemy
                ? StackMultiplier
                : Master.EffectValue;
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
            if (buffData.ConditionType == BuffConditionType.HpBelow)
            {
                state.IsConditionActive = _currentHealthRatio <= buffData.ConditionValue;
            }

            _buffStates.Add(state);
        }

        public void NotifyHit(int damagedId)
        {
            foreach (var state in _buffStates)
            {
                switch (state.Master.ConditionType)
                {
                    case BuffConditionType.HitCount:
                        state.HitCount++;
                        if (state.HitCount < state.Master.ConditionValue)
                        {
                            break;
                        }

                        // 発動: カウンタをリセットし、効果時間をリフレッシュ（スタックはしない）
                        state.HitCount = 0;
                        state.RemainingTime = state.Master.Duration;
                        break;

                    case BuffConditionType.HitDifferentEnemy:
                        if (damagedId == state.LastHitEnemyId)
                        {
                            // 同一敵への連続命中 → 積み上げた倍率をリセット
                            state.StackMultiplier = 1f;
                        }
                        else
                        {
                            // 直前と異なる敵への命中 → 増分(ConditionValue)を加算し上限(EffectValue)でクランプ
                            state.StackMultiplier = Mathf.Min(
                                state.StackMultiplier + state.Master.ConditionValue,
                                state.Master.EffectValue);
                        }

                        state.LastHitEnemyId = damagedId;
                        break;
                }
            }
        }

        public void NotifyDamageTaken(float damage)
        {
            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.OnDamaged)
                {
                    continue;
                }

                // 被弾ダメージ量 × レベル倍率(ConditionValue) だけ効果時間を延長（被弾のたびに累積）
                state.RemainingTime += damage * state.Master.ConditionValue;
            }
        }

        public void SetHealthRatio(float healthRatio)
        {
            _currentHealthRatio = healthRatio;

            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.HpBelow)
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

                result *= state.EffectMultiplier;
            }

            return result;
        }

        public float CalcPenetrationMultiply(int penetrationIndex)
        {
            var result = 1f;
            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.PenetrationCount)
                {
                    continue;
                }

                // ConditionValue体貫通するごとに(EffectValue - 1)を加算した倍率を掛ける
                // 例: 間隔3・倍率1.3なら 1〜2体目=1.0倍, 3〜5体目=1.3倍, 6〜8体目=1.6倍
                var interval = Mathf.Max(1, Mathf.RoundToInt(state.Master.ConditionValue));
                var stackCount = penetrationIndex / interval;
                result *= 1f + stackCount * (state.Master.EffectValue - 1f);
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
