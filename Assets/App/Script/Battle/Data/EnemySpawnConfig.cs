using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 雑魚湧きの調整パラメータ。
    /// 周期スポーンとは別に、生存数が最低数を下回ったらコモンを即時に補充する。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Config/EnemySpawnConfig")]
    public class EnemySpawnConfig : ScriptableObject
    {
        [SerializeField, Min(0), Tooltip("スポーン可能な間、生存中の敵（撃破演出中を除く）がこの数を下回ったら即時にコモンを補充する。0で無効")]
        private int _minimumAliveEnemyCount = 5;

        public int MinimumAliveEnemyCount => _minimumAliveEnemyCount;
    }
}
