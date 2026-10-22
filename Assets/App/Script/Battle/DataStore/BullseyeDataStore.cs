using App.Battle.Data;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ブルズアイ。1発の弾が敵を1体貫通するごとに、ダメージ倍率に (Value1 - 1) を加算する。
    /// 例: Value1=1.3 なら 1体目=1.0倍, 2体目=1.3倍, 3体目=1.6倍。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class BullseyeDataStore : IHitDamageMultiplierHook
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public BullseyeDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public int Order => HitHookOrder.Bullseye;

        /// <summary>同一弾内で何体目のヒットか（<see cref="HitData.PenetrationIndex"/>、1始まり）に応じた倍率。未所持・1体目は 1.0f</summary>
        public float GetDamageMultiplier(HitData hitData)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Bullseye, out var upgrade))
            {
                return 1f;
            }

            // 貫通した数（＝何体目か - 1）だけ倍率が上がる。1体目は常に1.0倍
            var penetratedCount = hitData.PenetrationIndex - 1;
            if (penetratedCount <= 0)
            {
                return 1f;
            }

            return 1f + penetratedCount * (upgrade.Value1.value - 1f);
        }
    }
}
