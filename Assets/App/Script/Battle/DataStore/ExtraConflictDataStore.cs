using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// エクスコンフリクトの実行時状態。
    /// 所持中はワルツ・マージ・フォーカスが封印され、代わりに Value1（ダメージ倍率）と
    /// Value2（クールダウン倍率）が弾性能へ掛かる。
    /// </summary>
    public class ExtraConflictDataStore : IExtraConflictDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public ExtraConflictDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        private bool IsActive =>
            _upgradeEffectSimpleCalculatorDataStore
                .TryGetHighestLevelUpgrade(UpgradeType.ExtraConflict, out _);

        public bool IsFocusLocked => IsActive;

        public bool IsShotTypeLocked(ShotType shotType)
        {
            if (shotType == ShotType.Normal)
            {
                return false;
            }

            return IsActive;
        }

        public float GetDamageMultiplier()
        {
            return !_upgradeEffectSimpleCalculatorDataStore
                .TryGetHighestLevelUpgrade(UpgradeType.ExtraConflict, out var upgrade)
                ? 1f
                : upgrade.Value1.value;
        }

        public float GetCoolDownMultiplier()
        {
            return !_upgradeEffectSimpleCalculatorDataStore
                .TryGetHighestLevelUpgrade(UpgradeType.ExtraConflict, out var upgrade)
                ? 1f
                : upgrade.Value2.value;
        }
    }
}
