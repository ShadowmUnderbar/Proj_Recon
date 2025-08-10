using System.Linq;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Database/EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private EnemyMasterData[] _enemyMasterDataList;

        public EnemyMasterData[] EnemyMasterData => _enemyMasterDataList;

        public bool TryGetEnemyMasterData(string enemyCode, out EnemyMasterData enemyMasterData)
        {
            enemyMasterData =
                _enemyMasterDataList.FirstOrDefault(enemyMasterData => enemyMasterData.EnemyCode == enemyCode);
            return enemyMasterData != null;
        }
    }
}