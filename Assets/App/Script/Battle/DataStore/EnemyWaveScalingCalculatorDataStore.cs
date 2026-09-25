using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ウェーブ進行による敵強化倍率の算出。
    /// ウェーブ1を等倍とし、1ウェーブ進むごとにそのウェーブ帯の増加率を加算していく（線形）。
    /// 増加率は WaveScalingData のウェーブ帯テーブルで切り替えられるため、
    /// 「5ウェーブ目からは伸びを急にする」といった調整をスプレッドシート側で行える。
    /// </summary>
    public class EnemyWaveScalingCalculatorDataStore : IEnemyWaveScalingCalculatorDataStore
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        // 適用開始ウェーブの昇順に並べた段階一覧（区切りの算出が並び順に依存するため）
        private readonly List<WaveScalingMasterData> _ascendingTiers;

        [Inject]
        public EnemyWaveScalingCalculatorDataStore(
            IWaveManagerDataStore waveManagerDataStore,
            WaveScalingDatabase waveScalingDatabase
        )
        {
            _waveManagerDataStore = waveManagerDataStore;

            _ascendingTiers = waveScalingDatabase.WaveScalingMasterData
                .Where(tier => tier != null)
                .OrderBy(tier => tier.Wave)
                .ToList();
        }

        public void GetScaledStatus(float baseHp, float baseDamage, out float scaledHp, out float scaledDamage)
        {
            CalculateMultipliers(out var hpMultiplier, out var damageMultiplier);

            // HPが0になると出現と同時に撃破扱いになるため、最低1は保証する
            scaledHp = Mathf.Max(1f, ApplyCeil(baseHp, hpMultiplier));

            // 攻撃力は0のマスターデータ（攻撃しない敵）があるため下限を設けない
            scaledDamage = ApplyCeil(baseDamage, damageMultiplier);
        }

        /// <summary>
        /// 倍率を掛けて小数を切り上げる。
        /// 基礎値が小さい敵（攻撃力1〜2など）でもウェーブごとに確実に伸びるよう、端数は切り上げる
        /// </summary>
        private static float ApplyCeil(float baseValue, double multiplier)
        {
            var scaled = baseValue * multiplier;
            return (float)System.Math.Ceiling(scaled - GetCeilTolerance(scaled));
        }

        /// <summary>
        /// 切り上げの許容誤差。0.1のような値はfloatで正確に表せず、
        /// 積算結果が 110 のつもりで 110.00002 になると切り上げが1つ余分に進むため、
        /// 誤差ぶんだけ削ってから切り上げる
        /// </summary>
        private static double GetCeilTolerance(double value)
        {
            const double RelativeTolerance = 1e-5d;
            const double MinimumTolerance = 1e-5d;
            return System.Math.Abs(value) * RelativeTolerance + MinimumTolerance;
        }

        /// <summary>
        /// 現在のウェーブまでに進んだ各ウェーブについて、そのウェーブが属する段階の増加率を足し合わせる。
        /// 段階の切り替わりで倍率が飛ばないよう、段階ごとの区間の長さで加算する
        /// </summary>
        private void CalculateMultipliers(out double hpMultiplier, out double damageMultiplier)
        {
            hpMultiplier = 1d;
            damageMultiplier = 1d;

            var currentWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            for (var i = 0; i < _ascendingTiers.Count; i++)
            {
                // ウェーブ1は等倍なので、増加が乗るのはウェーブ2以降
                var fromWave = Mathf.Max(2, _ascendingTiers[i].Wave);

                // 次の段階の開始ウェーブの手前までがこの段階の担当区間
                var toWave = i + 1 < _ascendingTiers.Count
                    ? _ascendingTiers[i + 1].Wave - 1
                    : currentWave;
                toWave = Mathf.Min(toWave, currentWave);

                if (toWave < fromWave)
                {
                    continue;
                }

                var waveCount = toWave - fromWave + 1;
                hpMultiplier += (double)_ascendingTiers[i].HpBuff * waveCount;
                damageMultiplier += (double)_ascendingTiers[i].AtkBuff * waveCount;
            }

            // 倍率が負になると値が反転するため下限を0で止める
            hpMultiplier = System.Math.Max(0d, hpMultiplier);
            damageMultiplier = System.Math.Max(0d, damageMultiplier);
        }
    }
}
