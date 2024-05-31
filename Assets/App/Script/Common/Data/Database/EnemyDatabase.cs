using System.Linq;
using App.Script.Common.Data.MasterData;
using UnityEngine;

namespace App.Script.Common.Data.Database
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Database/EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private EnemyMasterData[] _enemyMasterDataList;

        public bool TryGetEnemyMasterData(uint enemyId ,out EnemyMasterData enemyMasterData)
        {
            enemyMasterData = _enemyMasterDataList.FirstOrDefault(enemyMasterData => enemyMasterData.Id == enemyId);
            return enemyMasterData != null;
        }
    }
}