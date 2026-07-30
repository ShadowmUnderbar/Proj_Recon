using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 雪崩の実行時状態。
    /// マージショットのクールダウンは通常 Value1 倍に伸びる（基本弱体化）が、
    /// 直前のマージショットが敵に命中していれば次の1発だけ Value2 倍に短縮される。
    /// 当て続ける限り高速連射が維持され、外すと弱体状態に戻る。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class AvalancheDataStore : IAvalancheDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        // 直前のマージショットが命中していたか（次発の強化フラグ）
        private bool _isPreviousMergeShotHit;

        [Inject]
        public AvalancheDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public float GetCoolDownMultiplier(ShotType shotType)
        {
            if (shotType != ShotType.Merge)
            {
                return 1f;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Avalanche, out var upgrade))
            {
                return 1f;
            }

            return _isPreviousMergeShotHit
                ? upgrade.Value2.value
                : upgrade.Value1.value;
        }

        public void NotifyShot(ShotType shotType)
        {
            if (shotType != ShotType.Merge)
            {
                return;
            }

            // 強化フラグはこの1発で消費する（命中し続ければ命中通知で再度立つ）
            _isPreviousMergeShotHit = false;
        }

        public void NotifyMergeHit()
        {
            _isPreviousMergeShotHit = true;
        }
    }
}
