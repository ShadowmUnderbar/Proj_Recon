using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// コンフリクト系アップグレードの実行時状態。
    /// 何を封印するかは種別ごとに固定で、強化量は Value1（ダメージ倍率）・Value2（クールダウン倍率）で持つ。
    /// 複数所持した場合、封印は重ね掛けされ、倍率は掛け合わせる。
    /// </summary>
    public class ShotConflictDataStore : IShotConflictDataStore
    {
        /// <summary>コンフリクト1種が何を封印するかの定義</summary>
        private readonly struct ConflictDefinition
        {
            public ConflictDefinition(UpgradeType upgradeType, bool lockWaltz, bool lockMerge, bool lockFocus,
                bool lockAkimbo = false)
            {
                UpgradeType = upgradeType;
                LockWaltz = lockWaltz;
                LockMerge = lockMerge;
                LockFocus = lockFocus;
                LockAkimbo = lockAkimbo;
            }

            public UpgradeType UpgradeType { get; }
            public bool LockWaltz { get; }
            public bool LockMerge { get; }
            public bool LockFocus { get; }

            /// <summary>二丁拳銃を封印するか（利き手のみの射撃に制限する）</summary>
            public bool LockAkimbo { get; }
        }

        // 新しいコンフリクトを足すときはここに1行追加する（消費側の変更は不要）
        private static readonly ConflictDefinition[] Definitions =
        {
            new(UpgradeType.ExtraConflict, lockWaltz: true, lockMerge: true, lockFocus: true, lockAkimbo: true),
            new(UpgradeType.FocusConflict, lockWaltz: false, lockMerge: false, lockFocus: true),
            new(UpgradeType.MergeConflict, lockWaltz: false, lockMerge: true, lockFocus: false),
            new(UpgradeType.WaltzConflict, lockWaltz: true, lockMerge: false, lockFocus: false),
        };

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public ShotConflictDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public bool IsFocusLocked
        {
            get
            {
                foreach (var definition in Definitions)
                {
                    if (definition.LockFocus && IsOwned(definition.UpgradeType))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsAkimboLocked
        {
            get
            {
                foreach (var definition in Definitions)
                {
                    if (definition.LockAkimbo && IsOwned(definition.UpgradeType))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsShotTypeLocked(ShotType shotType)
        {
            if (shotType == ShotType.Normal)
            {
                return false;
            }

            foreach (var definition in Definitions)
            {
                // 未知のフォームを封印扱いにしないため、明示的に列挙する
                var locksThisShot = shotType switch
                {
                    ShotType.Waltz => definition.LockWaltz,
                    ShotType.Merge => definition.LockMerge,
                    _ => false,
                };

                if (locksThisShot && IsOwned(definition.UpgradeType))
                {
                    return true;
                }
            }

            return false;
        }

        public float GetDamageMultiplier()
        {
            var multiplier = 1f;

            foreach (var definition in Definitions)
            {
                if (TryGetUpgrade(definition.UpgradeType, out var upgrade))
                {
                    multiplier *= upgrade.Value1.value;
                }
            }

            return multiplier;
        }

        public float GetCoolDownMultiplier()
        {
            var multiplier = 1f;

            foreach (var definition in Definitions)
            {
                if (TryGetUpgrade(definition.UpgradeType, out var upgrade))
                {
                    multiplier *= upgrade.Value2.value;
                }
            }

            return multiplier;
        }

        private bool IsOwned(UpgradeType upgradeType)
        {
            return _upgradeEffectSimpleCalculatorDataStore.TryGetHighestLevelUpgrade(upgradeType, out _);
        }

        private bool TryGetUpgrade(UpgradeType upgradeType, out Common.Data.MasterData.UpgradeMasterData upgrade)
        {
            return _upgradeEffectSimpleCalculatorDataStore.TryGetHighestLevelUpgrade(upgradeType, out upgrade);
        }
    }
}
