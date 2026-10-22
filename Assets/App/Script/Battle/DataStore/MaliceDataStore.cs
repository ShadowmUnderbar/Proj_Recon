using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// マリスの効果判定。
    /// プレイヤーから半径 Value1 m 以内（水平距離）の敵について、
    /// ・ウイルスの残り効果時間を減らさない（範囲内にいる間はどのウイルスも切れない）
    /// ・1秒ごとに、ウイルスが無ければ威力 Value2・効果時間 Value3 秒で付け、あれば威力だけ Value2 足す（効果時間には触れない）
    /// ので、範囲内にいる間は威力が毎秒積み上がる（ユーザー確認済みの仕様）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class MaliceDataStore : IMaliceDataStore, IRunResettable
    {
        // 威力を足す間隔（秒）。issue の「1秒毎」
        private const float Interval = 1f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IVirusDataStore _virusDataStore;

        // 前回威力を足してからの経過時間
        private float _timer;

        // 範囲内の敵（列挙中に付与しないよう使い回す）
        private readonly List<int> _targetEnemyIds = new();

        [Inject]
        public MaliceDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore,
            IVirusDataStore virusDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _playerStateDataStore = playerStateDataStore;
            _enemyDataStore = enemyDataStore;
            _virusDataStore = virusDataStore;
        }

        public void Tick(float deltaTime)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Malice, out var upgrade))
            {
                _timer = 0f;
                return;
            }

            CollectTargets(upgrade.Value1.value);

            _timer += deltaTime;
            if (_timer >= Interval)
            {
                _timer -= Interval;

                // 威力・効果時間が0以下なら VirusDataStore 側で何もしない
                foreach (var enemyId in _targetEnemyIds)
                {
                    _virusDataStore.AddPower(enemyId, upgrade.Value2.value, upgrade.Value3.value);
                }
            }

            // 付けたばかりのウイルスも含めて、範囲内の敵のウイルスはこのフレーム切れないようにする
            foreach (var enemyId in _targetEnemyIds)
            {
                _virusDataStore.HoldRemainingTime(enemyId);
            }
        }

        private void CollectTargets(float radius)
        {
            _targetEnemyIds.Clear();

            if (radius <= 0f)
            {
                return;
            }

            var center = _playerStateDataStore.Position.Value;
            var sqrRadius = radius * radius;

            foreach (var enemy in _enemyDataStore.Enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                // プレイヤーの座標は足元基準なので、高さは見ずに水平距離で判定する
                var offset = enemy.Pose.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                _targetEnemyIds.Add(enemy.Id);
            }
        }

        public void ResetRun()
        {
            _timer = 0f;
        }
    }
}
