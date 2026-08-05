using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// スネークアイズの実行時状態。
    /// 視界中央（半径 Value2）に捉えている敵は移動・行動抽選の速度が Value1 倍になり、
    /// 範囲外に出てから Value3 秒後に元へ戻る（再度捉え直せば猶予はリセットされる）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class SnakeEyesDataStore : ISnakeEyesDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        // 減速中の敵ID → 効果解除までの残り時間（注視中は毎フレーム猶予時間まで戻される）
        private readonly Dictionary<int, float> _slowRemainingTime = new();

        // 毎フレームの注視IDを引き当てるための作業用セット（アロケーション回避のため再利用）
        private readonly HashSet<int> _gazedEnemyIds = new();

        // 速度倍率が変化した敵の通知リスト（同上）
        private readonly List<(int enemyId, float speedMultiplier)> _changes = new();

        // 効果が切れた敵IDの一時退避（辞書の列挙中に削除できないため）
        private readonly List<int> _releasedEnemyIds = new();

        [Inject]
        public SnakeEyesDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public bool TryGetGazeRadius(out float radius)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.SnakeEyes, out var upgrade))
            {
                radius = 0f;
                return false;
            }

            radius = upgrade.Value2.value;
            return true;
        }

        public IReadOnlyList<(int enemyId, float speedMultiplier)> UpdateGazedEnemies(
            IReadOnlyList<int> gazedEnemyIds)
        {
            _changes.Clear();

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.SnakeEyes, out var upgrade))
            {
                return _changes;
            }

            var speedMultiplier = upgrade.Value1.value;
            var releaseDelay = upgrade.Value3.value;

            _gazedEnemyIds.Clear();
            for (var i = 0; i < gazedEnemyIds.Count; i++)
            {
                _gazedEnemyIds.Add(gazedEnemyIds[i]);
            }

            // 範囲外の敵は解除までの残り時間を減らし、0になったら元の速度へ戻す
            _releasedEnemyIds.Clear();
            foreach (var enemyId in _slowRemainingTime.Keys)
            {
                if (_gazedEnemyIds.Contains(enemyId))
                {
                    continue;
                }

                _releasedEnemyIds.Add(enemyId);
            }

            foreach (var enemyId in _releasedEnemyIds)
            {
                var remainingTime = _slowRemainingTime[enemyId] - Time.deltaTime;
                if (remainingTime > 0f)
                {
                    _slowRemainingTime[enemyId] = remainingTime;
                    continue;
                }

                _slowRemainingTime.Remove(enemyId);
                _changes.Add((enemyId, 1f));
            }

            // 注視中の敵は減速を開始（または猶予時間をリセット）する
            foreach (var enemyId in _gazedEnemyIds)
            {
                if (!_slowRemainingTime.ContainsKey(enemyId))
                {
                    _changes.Add((enemyId, speedMultiplier));
                }

                _slowRemainingTime[enemyId] = releaseDelay;
            }

            return _changes;
        }

        public void RemoveEnemy(int enemyId)
        {
            _slowRemainingTime.Remove(enemyId);
        }
    }
}
