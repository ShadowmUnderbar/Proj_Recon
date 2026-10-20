using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 毒牙の効果判定。
    /// 回避中に体が触れた敵へ接触ダメージ（回避時跳ね返し攻撃）が入ったとき、その敵にウイルスを付与する。
    /// Value1 = ウイルスの威力 / Value2 = ウイルスの効果時間（秒）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class DokugaDataStore : IDokugaDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IVirusDataStore _virusDataStore;

        [Inject]
        public DokugaDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IVirusDataStore virusDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _virusDataStore = virusDataStore;
        }

        public void Infect(int enemyId)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Dokuga, out var upgrade))
            {
                return;
            }

            var power = upgrade.Value1.value;
            var duration = upgrade.Value2.value;
            if (power <= 0f || duration <= 0f)
            {
                return;
            }

            _virusDataStore.Apply(enemyId, power, duration);
        }
    }
}
