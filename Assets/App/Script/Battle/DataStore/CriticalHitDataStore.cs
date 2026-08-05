using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// クリティカルヒットの実行時判定。
    /// クリティカル確率は各アップグレードの確率同士を合算し（ダメージへの加減算はしない）、
    /// 確率100%ごとに1段が確定・端数分は抽選でさらに1段上乗せする。
    /// 1段につき最終ダメージが+100%（1段＝200%、2段＝300%）。
    /// 例: 確率350% → 基本400%ダメージ、50%の確率で500%ダメージ。
    /// いずれのアップグレードもレベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class CriticalHitDataStore : ICriticalHitDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;

        // クリティカル1段あたりのダメージ増加量（1段で元ダメージの200%になる）
        private const float CriticalDamageStepRate = 1f;

        [Inject]
        public CriticalHitDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IPlayerStateDataStore playerStateDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _playerStateDataStore = playerStateDataStore;
        }

        public float GetDamageMultiplier(HitData hitData)
        {
            var chance = GetCriticalChance(hitData);
            if (chance <= 0f)
            {
                return 1f;
            }

            // 確率100%ごとに1段確定し、端数分は抽選でさらに1段上乗せする
            var stepCount = Mathf.FloorToInt(chance);
            var fraction = chance - stepCount;
            if (fraction > 0f && Random.value < fraction)
            {
                stepCount++;
            }

            return 1f + stepCount * CriticalDamageStepRate;
        }

        /// <summary>
        /// この命中に適用されるクリティカル確率（1.0＝100%）を合算して返す。
        /// </summary>
        private float GetCriticalChance(HitData hitData)
        {
            var chance = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.LuckyChance);
            chance += GetTurnTableChance();
            chance += GetKillingCallChance(hitData);

            return Mathf.Max(0f, chance);
        }

        /// <summary>
        /// ターンテーブル: プレイヤーのHP減少割合に比例した確率を返す（Value1＝HP全損時の確率）。
        /// 例: Value1=0.5 でHPが半分まで減っていれば 25%。
        /// </summary>
        private float GetTurnTableChance()
        {
            var rate = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.TurnTable);
            if (rate <= 0f)
            {
                return 0f;
            }

            var maxHealth = _playerStateDataStore.MaxHealth.Value;
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            var lossRatio = Mathf.Clamp01(1f - _playerStateDataStore.Health.Value / maxHealth);
            return lossRatio * rate;
        }

        /// <summary>
        /// キリングコール: フォーカスショットの弾が、フォーカス対象以外に当たる前に対象へ命中した場合のみ確率を返す。
        /// </summary>
        private float GetKillingCallChance(HitData hitData)
        {
            if (hitData.FocusType != AimFocusType.Focus)
            {
                return 0f;
            }

            if (!hitData.IsFocusTarget)
            {
                return 0f;
            }

            // 同一弾の1体目＝フォーカス対象以外にはまだ命中していない
            if (hitData.PenetrationIndex != 1)
            {
                return 0f;
            }

            return _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.KillingCall);
        }
    }
}
