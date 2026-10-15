using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// サモンタレットの実行時状態（召喚のクールダウン）。
    /// Value1 = クールダウン秒数。レベルは累積せず、所持中の最高レベルのみを採用する。
    /// ラン開始直後とウェーブ終了後は、最初のマージショットですぐ召喚できる
    /// </summary>
    public class SummonTurretDataStore : ISummonTurretDataStore, IRunResettable, ITickable
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IFreezeDataStore _freezeDataStore;

        public float RemainingCoolDown { get; private set; }

        [Inject]
        public SummonTurretDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IFreezeDataStore freezeDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _freezeDataStore = freezeDataStore;
        }

        public void Tick()
        {
            if (RemainingCoolDown <= 0f)
            {
                return;
            }

            // ショップ中（ウェーブ間ポーズ）とフリーズ中は射撃と同じくクールダウンを進めない
            if (_waveManagerDataStore.IsWavePause.Value || _freezeDataStore.IsFreezing.CurrentValue)
            {
                return;
            }

            RemainingCoolDown = Mathf.Max(0f, RemainingCoolDown - Time.deltaTime);
        }

        public bool CanSummon()
        {
            return RemainingCoolDown <= 0f &&
                   _upgradeEffectSimpleCalculatorDataStore.TryGetHighestLevelUpgrade(UpgradeType.SummonTurret, out _);
        }

        public void StartCoolDown()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.SummonTurret, out var upgrade))
            {
                return;
            }

            RemainingCoolDown = upgrade.Value1.value;
        }

        public void ResetCoolDown()
        {
            RemainingCoolDown = 0f;
        }

        public void ResetRun()
        {
            ResetCoolDown();
        }
    }
}
