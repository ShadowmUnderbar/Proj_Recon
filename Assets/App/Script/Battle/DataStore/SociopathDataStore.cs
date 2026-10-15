using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ソシオパス: レイジ発動中に敵を撃破すると、レイジの効果時間を Value1 秒延ばす。
    /// レイジが発動していなければ何もしない（撃破でレイジを新たに発動させない）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class SociopathDataStore : ISociopathDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;

        [Inject]
        public SociopathDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
        }

        public float NotifyKill()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Sociopath, out var upgrade))
            {
                return 0f;
            }

            if (!_buffStateDataStore.IsRageActive)
            {
                return 0f;
            }

            var seconds = Mathf.Max(0f, upgrade.Value1.value);
            _buffStateDataStore.ExtendRage(seconds);
            return seconds;
        }
    }
}
