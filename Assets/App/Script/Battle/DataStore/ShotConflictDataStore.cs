using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// コンフリクト系アップグレードの実行時状態。
    /// 何を封印するかは種別ごとに固定で、強化量は Value1（ダメージ倍率）・Value2（クールダウン倍率）で持つ。
    /// 複数所持した場合、封印は重ね掛けされ、倍率は掛け合わせる。
    /// 判定は毎フレーム参照されるため、所持内容が変わったときだけ再計算して保持する。
    /// </summary>
    public class ShotConflictDataStore : IShotConflictDataStore, IInitializable, IDisposable
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
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;

        private readonly CompositeDisposable _disposables = new();

        private bool _isWaltzLocked;
        private bool _isMergeLocked;
        private float _damageMultiplier = 1f;
        private float _coolDownMultiplier = 1f;

        [Inject]
        public ShotConflictDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
        }

        public bool IsFocusLocked { get; private set; }

        public bool IsAkimboLocked { get; private set; }

        public void Initialize()
        {
            Recalculate();

            _upgradeSessionDataStore.OnChanged
                .Subscribe(_ => Recalculate())
                .AddTo(_disposables);
        }

        public bool IsShotTypeLocked(ShotType shotType)
        {
            // 未知のフォームを封印扱いにしないため、明示的に列挙する
            return shotType switch
            {
                ShotType.Waltz => _isWaltzLocked,
                ShotType.Merge => _isMergeLocked,
                _ => false,
            };
        }

        public float GetDamageMultiplier()
        {
            return _damageMultiplier;
        }

        public float GetCoolDownMultiplier()
        {
            return _coolDownMultiplier;
        }

        private void Recalculate()
        {
            _isWaltzLocked = false;
            _isMergeLocked = false;
            IsFocusLocked = false;
            IsAkimboLocked = false;
            _damageMultiplier = 1f;
            _coolDownMultiplier = 1f;

            foreach (var definition in Definitions)
            {
                if (!_upgradeEffectSimpleCalculatorDataStore
                        .TryGetHighestLevelUpgrade(definition.UpgradeType, out var upgrade))
                {
                    continue;
                }

                _isWaltzLocked |= definition.LockWaltz;
                _isMergeLocked |= definition.LockMerge;
                IsFocusLocked |= definition.LockFocus;
                IsAkimboLocked |= definition.LockAkimbo;

                _damageMultiplier *= upgrade.Value1.value;
                _coolDownMultiplier *= upgrade.Value2.value;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
