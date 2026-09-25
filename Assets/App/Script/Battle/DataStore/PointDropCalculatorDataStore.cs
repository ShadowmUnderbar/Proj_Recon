using System.Collections.Generic;
using System.Linq;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 撃破した敵のドロップ量算出と、ドロップ量の粒子単位への分割を行う。
    /// </summary>
    public class PointDropCalculatorDataStore : IPointDropCalculatorDataStore
    {
        private readonly PointDropConfig _pointDropConfig;
        private readonly PointParticleConfig _pointParticleConfig;

        // 値の大きい順に並べた単位一覧（分割は大単位から貪欲に取るため、この順序に依存する）
        private readonly List<PointUnitData> _descendingUnits;

        // 単位ごとの個数バッファ（分割のたびに確保しないよう使い回す）
        private readonly int[] _unitCounts;

        [Inject]
        public PointDropCalculatorDataStore(PointDropConfig pointDropConfig, PointParticleConfig pointParticleConfig)
        {
            _pointDropConfig = pointDropConfig;
            _pointParticleConfig = pointParticleConfig;

            // 値が0以下の単位は分割が終わらなくなるため除外する
            _descendingUnits = _pointParticleConfig.Units
                .Where(unit => unit != null && unit.Value > 0)
                .OrderByDescending(unit => unit.Value)
                .ToList();

            _unitCounts = new int[_descendingUnits.Count];
        }

        /// <summary>
        /// 撃破した敵のドロップ量。個別設定（1以上）があればそれを、無ければランク別の既定値を使う
        /// </summary>
        public int GetDropPoint(EnemyMasterData enemyMasterData)
        {
            if (enemyMasterData == null)
            {
                return 0;
            }

            return enemyMasterData.DropPointOverride > 0
                ? enemyMasterData.DropPointOverride
                : _pointDropConfig.GetPoint(enemyMasterData.EnemyRankType);
        }

        /// <summary>
        /// ドロップ量を粒子の単位へ分割して results に詰める（呼び出し側のリストを使い回すためアロケーションしない）。
        /// 大きい単位から貪欲に取り、粒子数が上限を超える場合は小さい単位から一つ上の単位へ繰り上げる。
        /// 繰り上げは切り上げのため、分割後の合計は amount を下回らない。
        /// </summary>
        public void Split(int amount, List<PointUnitData> results)
        {
            results.Clear();

            if (amount <= 0 || _descendingUnits.Count == 0)
            {
                return;
            }

            var remaining = amount;

            for (var i = 0; i < _descendingUnits.Count; i++)
            {
                var unitValue = _descendingUnits[i].Value;
                _unitCounts[i] = remaining / unitValue;
                remaining -= _unitCounts[i] * unitValue;
            }

            // 最小単位で割り切れない端数は、取りこぼしにならないよう最小単位1個分として切り上げる
            if (remaining > 0)
            {
                _unitCounts[_descendingUnits.Count - 1]++;
            }

            MergeToMaxCount();

            for (var i = 0; i < _descendingUnits.Count; i++)
            {
                for (var count = 0; count < _unitCounts[i]; count++)
                {
                    results.Add(_descendingUnits[i]);
                }
            }
        }

        /// <summary>
        /// 粒子数が上限を超えている間、最小の単位から順に一つ上の単位へ繰り上げる。
        /// 1回の繰り上げで必ず1段階ぶんの単位が消えるため、最大でも単位数ぶんの回数で終わる。
        /// 最大単位まで繰り上げても上限を超える場合（極端なドロップ量）はそのまま出す。
        /// </summary>
        private void MergeToMaxCount()
        {
            for (var i = _descendingUnits.Count - 1; i >= 1; i--)
            {
                if (GetTotalCount() <= _pointParticleConfig.MaxParticleCount)
                {
                    return;
                }

                if (_unitCounts[i] == 0)
                {
                    continue;
                }

                var mergedValue = _unitCounts[i] * _descendingUnits[i].Value;
                _unitCounts[i - 1] += Mathf.CeilToInt(mergedValue / (float)_descendingUnits[i - 1].Value);
                _unitCounts[i] = 0;
            }
        }

        private int GetTotalCount()
        {
            var total = 0;

            for (var i = 0; i < _unitCounts.Length; i++)
            {
                total += _unitCounts[i];
            }

            return total;
        }
    }
}
