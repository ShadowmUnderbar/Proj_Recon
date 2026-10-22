using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ジャイアントキリング（撃破時回復）。撃破した敵のランクに応じて体力を回復する。
    /// Value1 = マイナー撃破時の回復量 / Value2 = メジャー撃破時の回復量。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class HealOnKillDataStore : IHealOnKillDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public HealOnKillDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        /// <summary>対象外ランク・未所持は 0</summary>
        public float GetHealAmount(EnemyRankType rankType)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.HealOnKill, out var upgrade))
            {
                return 0f;
            }

            return rankType switch
            {
                EnemyRankType.Minor => upgrade.Value1.value,
                EnemyRankType.Major => upgrade.Value2.value,
                _ => 0f
            };
        }
    }
}
