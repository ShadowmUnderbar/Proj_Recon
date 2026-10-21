using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// サブスク: ウェーブクリア時に所持キャッシュを Value1 倍にする（増えた分の端数は切り捨て）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class SubscribeDataStore : ISubscribeDataStore
    {
        // 1.3 が 1.2999… になるような誤差で、ちょうど割り切れるボーナスが1少なく切り捨てられないようにする
        private const float RoundingEpsilon = 0.001f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public SubscribeDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public int GetWaveClearBonus(int currentPoint)
        {
            if (currentPoint <= 0)
            {
                return 0;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Subscribe, out var upgrade))
            {
                return 0;
            }

            var bonusRate = Mathf.Max(0f, upgrade.Value1.value - 1f);
            return Mathf.FloorToInt(currentPoint * bonusRate + RoundingEpsilon);
        }
    }
}
