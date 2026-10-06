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
    /// WaveScalingData のウェーブ帯テーブルから、現在のウェーブが属する帯の倍率をそのまま使う（階段状）。
    /// 例: wave=3 の行が 1.2 なら、次の行の開始ウェーブの手前まで ×1.2 になる。
    /// 「5ウェーブ目からは×1.5」といった調整をスプレッドシート側で行える。
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
        /// 現在のウェーブが属する段階（開始ウェーブが現在以下のうち最も後ろの段階）の倍率をそのまま使う。
        /// 最初の段階より前のウェーブは等倍とする
        /// </summary>
        private void CalculateMultipliers(out double hpMultiplier, out double damageMultiplier)
        {
            hpMultiplier = 1d;
            damageMultiplier = 1d;

            var currentWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            foreach (var tier in _ascendingTiers)
            {
                // 昇順に並んでいるので、開始ウェーブを超えた時点で以降の段階は対象外
                if (tier.Wave > currentWave)
                {
                    break;
                }

                hpMultiplier = tier.HpBuff;
                damageMultiplier = tier.AtkBuff;
            }

            // 倍率が負になると値が反転するため下限を0で止める
            hpMultiplier = System.Math.Max(0d, hpMultiplier);
            damageMultiplier = System.Math.Max(0d, damageMultiplier);
        }
    }
}
