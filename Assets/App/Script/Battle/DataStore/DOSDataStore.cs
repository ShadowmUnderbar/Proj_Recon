using App.Battle.Data;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// DOS の効果判定。
    /// 射撃（プレイヤー・味方の弾）が敵に命中するたびに、確率でその敵にウイルスを付与する。
    /// Value1 = 付与する確率 / Value2 = ウイルスの威力 / Value3 = ウイルスの効果時間（秒）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class DOSDataStore : IHitEffectHook
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IVirusDataStore _virusDataStore;

        [Inject]
        public DOSDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IVirusDataStore virusDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _virusDataStore = virusDataStore;
        }

        public int Order => HitHookOrder.DOS;

        /// <summary>射撃（ShotType あり）の命中で、撃破されずに残った敵にだけ付与する</summary>
        public void OnAfterDamage(HitData hitData, float dealtDamage, bool isTargetAlive)
        {
            if (hitData.ShotType == null || !isTargetAlive)
            {
                return;
            }

            TryInfect(hitData.DamagedId);
        }

        /// <summary>所持中の最高レベルの確率（Value1）で、ウイルス（威力 Value2・効果時間 Value3 秒）を付与する</summary>
        private void TryInfect(int enemyId)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.DOS, out var upgrade))
            {
                return;
            }

            // Random.value は 0〜1 の両端を含むため、確率0では外れ・確率1では必ず当たるよう比較を分ける
            var chance = upgrade.Value1.value;
            if (chance <= 0f || (chance < 1f && Random.value >= chance))
            {
                return;
            }

            _virusDataStore.Apply(enemyId, upgrade.Value2.value, upgrade.Value3.value);
        }
    }
}
