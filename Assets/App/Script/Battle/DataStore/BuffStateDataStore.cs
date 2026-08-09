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

            // HitCount / OnDamaged / AfterDodge条件: 残り効果時間（秒）。0以下なら非アクティブ
            public float RemainingTime;

            // HpBelow条件: 条件成立中フラグ
            public bool IsConditionActive;

            // HitDifferentEnemy条件: 現在の累積効果倍率（初期値1=補正なし。上限は Master.EffectValue）
            public float StackMultiplier = 1f;

            // HitDifferentEnemy条件: 直前に命中した敵ID（-1=未命中）。同一敵の連続命中でリセット判定に使う
            public int LastHitEnemyId = -1;

            // KillWithDifferentForm条件: 次の撃破まで適用する倍率（1=補正なし）
            public float KillFormMultiplier = 1f;

            // KillWithDifferentForm条件: 直前の撃破フォーム（null=未撃破）
            public ShotType? LastKillShotType;

            // KillWithDifferentForm条件: 2つ前の撃破フォーム（null=未撃破）
            public ShotType? PreviousKillShotType;

            public bool IsActive => Master.ConditionType switch
            {
                BuffConditionType.HitCount => RemainingTime > 0f,
                BuffConditionType.HpBelow => IsConditionActive,
                BuffConditionType.HitDifferentEnemy => StackMultiplier > 1f,
                BuffConditionType.OnDamaged => RemainingTime > 0f,
                BuffConditionType.AfterDodge => RemainingTime > 0f,
                BuffConditionType.KillWithDifferentForm => KillFormMultiplier > 1f,
                // HP減少割合に比例する効果は常時発動（軽減量の算出側でHP割合を参照する）
                BuffConditionType.HpLossScaling => true,
                _ => false
            };

            // アクティブ時に適用する効果倍率。スタック型は累積倍率、それ以外はマスターの固定倍率
            public float EffectMultiplier => Master.ConditionType switch
            {
                BuffConditionType.HitDifferentEnemy => StackMultiplier,
                BuffConditionType.KillWithDifferentForm => KillFormMultiplier,
                _ => Master.EffectValue
            };
        }

        // 被ダメージ軽減率の上限（軽減しきってダメージが完全に無効化されるのを防ぐ）
        private const float MaxDamageReductionRate = 0.9f;

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

        public void NotifyKill(ShotType? shotType)
        {
            // 射撃以外（回避の突進など）の撃破はフォーム履歴に含めない
            if (!shotType.HasValue)
            {
                return;
            }

            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.KillWithDifferentForm)
                {
                    continue;
                }

                // 直前の撃破と異なるフォームか（履歴が無い初回は成立させない）
                var isDifferentFromLast = state.LastKillShotType.HasValue &&
                                          state.LastKillShotType.Value != shotType.Value;

                // 2つ前の撃破とも異なるフォームか
                var isDifferentFromPrevious = state.PreviousKillShotType.HasValue &&
                                              state.PreviousKillShotType.Value != shotType.Value;

                if (!isDifferentFromLast)
                {
                    // 直前と同じフォーム（または初回）なら補正なしに戻す
                    state.KillFormMultiplier = 1f;
                }
                else
                {
                    // 2つ前とも異なれば上位倍率(ConditionValue)、直前のみ異なれば通常倍率(EffectValue)
                    state.KillFormMultiplier = isDifferentFromPrevious
                        ? Mathf.Max(state.Master.ConditionValue, state.Master.EffectValue)
                        : state.Master.EffectValue;
                }

                state.PreviousKillShotType = state.LastKillShotType;
                state.LastKillShotType = shotType;
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

        public void NotifyDodge()
        {
            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.AfterDodge)
                {
                    continue;
                }

                // 回避のたびに効果時間をリフレッシュ（スタックはしない）
                state.RemainingTime = state.Master.Duration;
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

        public float CalcDamageTakenMultiply()
        {
            var result = 1f;
            foreach (var state in _buffStates)
            {
                if (state.Master.EffectType != BuffEffectType.DamageReduction || !state.IsActive)
                {
                    continue;
                }

                if (state.Master.ConditionType != BuffConditionType.HpLossScaling)
                {
                    continue;
                }

                // HP減少割合 × EffectValue を軽減率とする（上限 MaxDamageReductionRate でクランプ）
                // 例: EffectValue=1.0 でHPが半分まで減っていれば軽減率0.5＝被ダメージ半減
                var lossRatio = Mathf.Clamp01(1f - _currentHealthRatio);
                var reduction = Mathf.Clamp(lossRatio * state.Master.EffectValue, 0f, MaxDamageReductionRate);
                result *= 1f - reduction;
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
