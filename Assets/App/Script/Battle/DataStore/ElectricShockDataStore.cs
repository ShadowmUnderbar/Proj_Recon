using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 感電の実行時状態。
    /// ワルツショットが命中した敵を中心に半径内の敵を探し、命中ダメージの一定割合を伝播させる。
    /// 半径は Value3（基礎半径）× 判定サイズ強化 × Value1（レベルごとの半径倍率）で決まる。
    /// Value1 は半径全体に掛かるため、判定サイズ強化を持たない状態では Value3 × Value1 が実半径になる。
    /// </summary>
    public class ElectricShockDataStore : IElectricShockDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        // 伝播先Idの一時バッファ（呼び出しごとに詰め直す）
        private readonly List<int> _targetEnemyIds = new();

        [Inject]
        public ElectricShockDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public bool TryGetChain(HitData hitData, out ElectricShockChain chain)
        {
            chain = default;

            if (hitData.ShotType != ShotType.Waltz)
            {
                return false;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.ElectricShock, out var upgrade))
            {
                return false;
            }

            // 撃破済みの敵への命中は EnemyDataStore.Damage 側で無視されるため、伝播も起こさない
            if (!_enemyDataStore.TryGetEnemyData(hitData.DamagedId, out var hitEnemy) || hitEnemy.IsDead)
            {
                return false;
            }

            var radius = upgrade.Value3.value
                         * _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.HitRange)
                         * upgrade.Value1.value;

            var chainDamage = hitData.Damage * upgrade.Value2.value;

            if (radius <= 0f || chainDamage <= 0f)
            {
                return false;
            }

            var center = hitEnemy.Pose.position;
            var sqrRadius = radius * radius;

            _targetEnemyIds.Clear();

            foreach (var enemy in _enemyDataStore.Enemies)
            {
                // 命中した敵自身と撃破済みの敵は対象外
                if (enemy.Id == hitData.DamagedId || enemy.IsDead)
                {
                    continue;
                }

                if ((enemy.Pose.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                _targetEnemyIds.Add(enemy.Id);
            }

            if (_targetEnemyIds.Count <= 0)
            {
                return false;
            }

            chain = new ElectricShockChain(_targetEnemyIds, chainDamage, center);
            return true;
        }
    }
}
