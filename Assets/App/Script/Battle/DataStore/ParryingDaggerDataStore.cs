using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// パリングダガーの実行時状態。
    /// Value1 は「パリィ弾が引き継ぐフォーム数」（1=ノーマル / 2=+ワルツ / 3=+マージ）を表し、
    /// レベルは累積せず所持中の最高レベルのみを採用する。
    /// </summary>
    public class ParryingDaggerDataStore : IParryingDaggerDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;

        [Inject]
        public ParryingDaggerDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
        }

        public bool IsActive =>
            _upgradeEffectSimpleCalculatorDataStore
                .TryGetHighestLevelUpgrade(UpgradeType.ParryingDagger, out _);

        public bool TryGetParryBulletData(out BulletData bulletData)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.ParryingDagger, out var upgrade))
            {
                bulletData = null;
                return false;
            }

            var inheritedFormCount = Mathf.Max(1, Mathf.RoundToInt(upgrade.Value1.value));
            bulletData = _playerBulletParameterDataStore.GetParryBulletData(inheritedFormCount);
            return true;
        }
    }
}
