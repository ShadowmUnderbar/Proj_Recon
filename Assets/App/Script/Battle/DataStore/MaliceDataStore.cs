using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// マリスの効果判定。
    /// 1秒ごとに、プレイヤーから半径 Value1 m 以内（水平距離）の敵へ、威力 Value2・効果時間 Value3 秒のウイルスを付与する。
    /// ウイルスは重ねると威力が加算されるので、範囲内にいる間は毎秒威力が積み上がる（ユーザー確認済みの仕様）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class MaliceDataStore : IMaliceDataStore, IRunResettable
    {
        // 付与の間隔（秒）。issue の「1秒毎」
        private const float Interval = 1f;

        // ウイルス側（VirusDataStore）と同じ誤差の許容。付け直しがウイルスの切れるのと同じフレームになっても先に付くようにそろえる
        private const float Epsilon = 1e-4f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IVirusDataStore _virusDataStore;

        // 前回の付与からの経過時間。付与したフレームの deltaTime を含めて数え、ウイルスの残り効果時間と同じ進み方にする
        private float _timer;

        // 付与する敵（列挙中に付与しないよう使い回す）
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

            _timer += deltaTime;
            if (_timer < Interval - Epsilon)
            {
                return;
            }

            // 付与したウイルスはこのフレームの deltaTime 分だけ直後に進むので、次の周期もこのフレームから数える
            _timer = deltaTime;

            Infect(upgrade.Value1.value, upgrade.Value2.value, upgrade.Value3.value);
        }

        private void Infect(float radius, float power, float duration)
        {
            if (radius <= 0f)
            {
                return;
            }

            var center = _playerStateDataStore.Position.Value;
            var sqrRadius = radius * radius;

            _targetEnemyIds.Clear();
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

            // 威力・効果時間が0以下なら Apply 側で何もしない
            foreach (var enemyId in _targetEnemyIds)
            {
                _virusDataStore.Apply(enemyId, power, duration);
            }
        }

        public void ResetRun()
        {
            _timer = 0f;
        }
    }
}
