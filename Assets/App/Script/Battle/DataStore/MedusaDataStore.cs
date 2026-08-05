using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// メデューサの実行時状態。
    /// 視界中央（半径 Value2）に捉えたメジャークラス以上の敵を Value1 秒スタンさせる。
    /// 一度スタンした敵には再発しない（同じ敵を捉え直しても発動しない）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class MedusaDataStore : IMedusaDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        // スタン中の敵ID → 残りスタン時間
        private readonly Dictionary<int, float> _stunRemainingTime = new();

        // 一度スタンさせた敵ID（再発防止）
        private readonly HashSet<int> _stunnedEnemyIds = new();

        // スタン状態が変化した敵の通知リスト（アロケーション回避のため再利用）
        private readonly List<(int enemyId, bool isStun)> _changes = new();

        // スタン中の敵IDの一時退避（辞書の列挙中に削除できないため）
        private readonly List<int> _activeEnemyIds = new();

        [Inject]
        public MedusaDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public bool TryGetGazeRadius(out float radius)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Medusa, out var upgrade))
            {
                radius = 0f;
                return false;
            }

            radius = upgrade.Value2.value;
            return true;
        }

        public bool IsStunTargetRank(EnemyRankType rankType)
        {
            // メジャークラス以上（コモン・マイナーは対象外）
            return rankType is EnemyRankType.Major or EnemyRankType.Boss or EnemyRankType.Irregular;
        }

        public IReadOnlyList<(int enemyId, bool isStun)> UpdateStunTargets(IReadOnlyList<int> stunTargetEnemyIds)
        {
            _changes.Clear();

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Medusa, out var upgrade))
            {
                return _changes;
            }

            // スタン中の敵の残り時間を進め、切れた敵を解除する
            _activeEnemyIds.Clear();
            foreach (var enemyId in _stunRemainingTime.Keys)
            {
                _activeEnemyIds.Add(enemyId);
            }

            foreach (var enemyId in _activeEnemyIds)
            {
                var remainingTime = _stunRemainingTime[enemyId] - Time.deltaTime;
                if (remainingTime > 0f)
                {
                    _stunRemainingTime[enemyId] = remainingTime;
                    continue;
                }

                _stunRemainingTime.Remove(enemyId);
                _changes.Add((enemyId, false));
            }

            // 未スタンの対象を新たにスタンさせる
            var duration = upgrade.Value1.value;
            for (var i = 0; i < stunTargetEnemyIds.Count; i++)
            {
                var enemyId = stunTargetEnemyIds[i];
                if (!_stunnedEnemyIds.Add(enemyId))
                {
                    continue;
                }

                _stunRemainingTime[enemyId] = duration;
                _changes.Add((enemyId, true));
            }

            return _changes;
        }

        public void RemoveEnemy(int enemyId)
        {
            _stunRemainingTime.Remove(enemyId);
            _stunnedEnemyIds.Remove(enemyId);
        }
    }
}
