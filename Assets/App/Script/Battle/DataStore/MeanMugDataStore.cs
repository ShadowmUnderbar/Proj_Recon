using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ガン飛ばしの実行時状態。
    /// 視界中央（半径 Value2）に捉えている敵は受ける最終ダメージが Value1 倍になる。
    /// 範囲外に出た敵は次のフレームで即座に効果外となる。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class MeanMugDataStore : IMeanMugDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        // 現在注視中の敵ID
        private readonly HashSet<int> _gazedEnemyIds = new();

        [Inject]
        public MeanMugDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public bool TryGetGazeRadius(out float radius)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.MeanMug, out var upgrade))
            {
                radius = 0f;
                return false;
            }

            radius = upgrade.Value2.value;
            return true;
        }

        public void SetGazedEnemies(IReadOnlyList<int> gazedEnemyIds)
        {
            _gazedEnemyIds.Clear();
            for (var i = 0; i < gazedEnemyIds.Count; i++)
            {
                _gazedEnemyIds.Add(gazedEnemyIds[i]);
            }
        }

        public float GetDamageMultiplier(int enemyId)
        {
            if (!_gazedEnemyIds.Contains(enemyId))
            {
                return 1f;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.MeanMug, out var upgrade))
            {
                return 1f;
            }

            return upgrade.Value1.value;
        }
    }
}
