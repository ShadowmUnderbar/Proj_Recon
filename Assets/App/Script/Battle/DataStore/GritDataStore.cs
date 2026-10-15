using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 食いしばりの効果判定。
    /// 致死ダメージを受けた際、そのダメージを無効化して体力1で耐える。
    /// Value1 = 1ウェーブに発動できる回数 / Value2 = レイジ発動中に回復する体力の倍率（レイジの残り秒数に掛ける）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class GritDataStore : IGritDataStore, IRunResettable
    {
        // 食いしばりで耐えたときに残る体力
        private const float SurviveHealth = 1f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        // 発動回数を数えているウェーブと、そのウェーブでの発動回数。ウェーブが変わったら数え直す
        private int _countingWave = -1;
        private int _activatedCount;

        [Inject]
        public GritDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public bool TryActivate(out float health)
        {
            health = 0f;

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Grit, out var upgrade))
            {
                return false;
            }

            var currentWave = _waveManagerDataStore.CurrentWave.CurrentValue;
            if (_countingWave != currentWave)
            {
                _countingWave = currentWave;
                _activatedCount = 0;
            }

            if (_activatedCount >= Mathf.RoundToInt(upgrade.Value1.value))
            {
                return false;
            }

            _activatedCount++;

            // レイジ発動中なら残り秒数 × Value2 を上乗せする（発動していなければ残り0秒なので体力1のまま）
            var heal = _buffStateDataStore.RageRemainingTime * Mathf.Max(0f, upgrade.Value2.value);
            health = SurviveHealth + heal;

            Debug.Log($"[GritDataStore] 致死ダメージを食いしばりで耐えました（ウェーブ{currentWave}で{_activatedCount}回目、回復量 {heal:F1}）");
            return true;
        }

        public void ResetRun()
        {
            _countingWave = -1;
            _activatedCount = 0;
        }
    }
}
