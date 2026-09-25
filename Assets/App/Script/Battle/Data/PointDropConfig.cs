using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 敵ランクごとのポイントドロップ量。
    /// 敵個別に調整したい場合は EnemyMasterData の DropPointOverride で上書きする。
    /// </summary>
    [CreateAssetMenu(fileName = "PointDropConfig", menuName = "Config/PointDropConfig")]
    public class PointDropConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Commonランクの敵を倒したときのドロップ量")]
        private int _commonPoint = 5;

        [SerializeField, Tooltip("Minorランクの敵を倒したときのドロップ量")]
        private int _minorPoint = 15;

        [SerializeField, Tooltip("Majorランクの敵を倒したときのドロップ量")]
        private int _majorPoint = 50;

        [SerializeField, Tooltip("Bossランクの敵を倒したときのドロップ量")]
        private int _bossPoint = 200;

        [SerializeField, Tooltip("Irregularランクの敵を倒したときのドロップ量")]
        private int _irregularPoint = 50;

        /// <summary>
        /// ランクごとの既定ドロップ量。未設定ランク（None）は0を返し、粒子を出さない
        /// </summary>
        public int GetPoint(EnemyRankType rankType)
        {
            return rankType switch
            {
                EnemyRankType.Common => _commonPoint,
                EnemyRankType.Minor => _minorPoint,
                EnemyRankType.Major => _majorPoint,
                EnemyRankType.Boss => _bossPoint,
                EnemyRankType.Irregular => _irregularPoint,
                _ => 0
            };
        }
    }
}
