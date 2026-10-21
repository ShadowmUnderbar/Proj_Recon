using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// コーラサワーの効果判定。
    /// 致死ダメージを受けた際、確率でそのダメージを無効化する。
    /// Value1 = 無効化する確率の初期値 / Value2 = 発動するたびに下がる確率。
    /// 発動回数はウェーブが変わると数え直す（確率が元に戻る）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class ColasourDataStore : IColasourDataStore, IRunResettable
    {
        // これ以下の確率は0とみなす（0.3 - 0.1 × 3 のような計算で、誤差のぶんだけ確率が残らないように）
        private const float ChanceEpsilon = 0.0001f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        // 発動回数を数えているウェーブと、そのウェーブでの発動回数。ウェーブが変わったら数え直す
        private int _countingWave = -1;
        private int _activatedCount;

        [Inject]
        public ColasourDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public float GetChance()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Colasour, out var upgrade))
            {
                return 0f;
            }

            RefreshCountingWave();

            var chance = upgrade.Value1.value - upgrade.Value2.value * _activatedCount;
            return chance <= ChanceEpsilon ? 0f : Mathf.Clamp01(chance);
        }

        public bool TryActivate()
        {
            var chance = GetChance();

            // Random.value は 0〜1 の両端を含むため、確率0では外れ・確率1では必ず当たるよう比較を分ける
            if (chance <= 0f || (chance < 1f && Random.value >= chance))
            {
                return false;
            }

            _activatedCount++;

            Debug.Log($"[ColasourDataStore] 致死ダメージをコーラサワーで無効化しました（ウェーブ{_countingWave}で{_activatedCount}回目、確率 {chance:P0}）");
            return true;
        }

        public void ResetRun()
        {
            _countingWave = -1;
            _activatedCount = 0;
        }

        private void RefreshCountingWave()
        {
            var currentWave = _waveManagerDataStore.CurrentWave.CurrentValue;
            if (_countingWave == currentWave)
            {
                return;
            }

            _countingWave = currentWave;
            _activatedCount = 0;
        }
    }
}
