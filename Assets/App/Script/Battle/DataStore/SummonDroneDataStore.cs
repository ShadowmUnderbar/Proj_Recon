using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// サモンドローンの実行時状態（召喚に向けた撃破数）。
    /// Value1 = 召喚に必要な撃破数 / Value2 = 1回に召喚する数。レベルは累積せず、所持中の最高レベルのみを採用する。
    /// 撃破数はウェーブをまたいで持ち越し、リスタートで0に戻す
    /// </summary>
    public class SummonDroneDataStore : ISummonDroneDataStore, IRunResettable
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        public int KillCount { get; private set; }

        [Inject]
        public SummonDroneDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public int NotifyKill()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.SummonDrone, out var upgrade))
            {
                return 0;
            }

            // 0以下の設定で毎撃破召喚にならないよう、最低1体にする
            var requiredKills = Mathf.Max(1, Mathf.RoundToInt(upgrade.Value1.value));

            KillCount++;
            if (KillCount < requiredKills)
            {
                return 0;
            }

            KillCount = 0;
            return Mathf.Max(0, Mathf.RoundToInt(upgrade.Value2.value));
        }

        public void ResetRun()
        {
            KillCount = 0;
        }
    }
}
