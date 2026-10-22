using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ラフレシアの効果判定。
    /// ウイルスを持ったまま撃破された敵から、半径 Value1 m 以内の他の敵へウイルスを広げる。
    /// 広げるウイルスは 威力 = 撃破された敵の威力 × Value2 / 効果時間 = 撃破された敵の残り効果時間 + Value3 秒。
    /// 広がった先の敵が撃破されれば、そこからさらに広がる。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class RafflesiaDataStore : IEnemyKillHook
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IVirusDataStore _virusDataStore;

        // 広げる先の敵（撃破のたびに確保しないよう使い回す）
        private readonly List<int> _targetEnemyIds = new();

        [Inject]
        public RafflesiaDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IEnemyDataStore enemyDataStore,
            IVirusDataStore virusDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _enemyDataStore = enemyDataStore;
            _virusDataStore = virusDataStore;
        }

        /// <summary>
        /// 撃破された敵がウイルスを持っていれば周りへ広げる。
        /// 撃破された敵の敵データ・ウイルスが取り除かれる前（死亡演出の前）に呼ばれる
        /// </summary>
        public void OnEnemyKilled(int deadEnemyId)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Rafflesia, out var upgrade))
            {
                return;
            }

            if (!_virusDataStore.TryGetVirus(deadEnemyId, out var virus))
            {
                return;
            }

            if (!_enemyDataStore.TryGetEnemyData(deadEnemyId, out var deadEnemy))
            {
                return;
            }

            var radius = upgrade.Value1.value;
            var power = virus.Power * upgrade.Value2.value;
            var duration = virus.RemainingTime + upgrade.Value3.value;

            // 威力・効果時間が0以下なら Apply 側で何もしない
            if (radius <= 0f)
            {
                return;
            }

            var center = deadEnemy.Pose.position;
            var sqrRadius = radius * radius;

            // Apply の通知の購読先が敵の一覧を変えても列挙が壊れないよう、先に対象を集めてから付与する
            _targetEnemyIds.Clear();
            foreach (var enemy in _enemyDataStore.Enemies)
            {
                // 撃破された敵自身と撃破済みの敵は対象外
                if (enemy.Id == deadEnemyId || enemy.IsDead)
                {
                    continue;
                }

                if ((enemy.Pose.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                _targetEnemyIds.Add(enemy.Id);
            }

            foreach (var enemyId in _targetEnemyIds)
            {
                _virusDataStore.Apply(enemyId, power, duration);
            }
        }
    }
}
