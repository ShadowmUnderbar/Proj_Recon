using System.Linq;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Database/EnemySpawnDatabase")]
    public class EnemySpawnDatabase : ScriptableObject
    {
        public SpawnTable[] SpawnTableList;

        public bool TryGetSpawnTable(UnlockCoreSkillType unlockCoreSkillType, out SpawnTable spawnTable)
        {
            spawnTable = SpawnTableList.FirstOrDefault(x => x.UnlockCoreSkillType == unlockCoreSkillType);
            return spawnTable != null;
        }

        [System.Serializable]
        public class SpawnTable
        {
            public UnlockCoreSkillType UnlockCoreSkillType;
            public SpawnGroup[] SpawnGroupList;

            public EnemyMasterData GetRandomSpawnEnemy(EnemyRankType rankType)
            {
                var filteredList = SpawnGroupList
                    .Where(x => x.SpawnTableList.EnemyRankType == rankType)
                    .ToArray();

                if (filteredList.Length == 0)
                {
                    return null;
                }

                var totalProbability = filteredList.Sum(x => x.Probability);
                var randomValue = Random.Range(0, totalProbability);
                var cumulativeProbability = 0;

                foreach (var group in filteredList)
                {
                    cumulativeProbability += group.Probability;
                    if (randomValue < cumulativeProbability)
                    {
                        return group.SpawnTableList;
                    }
                }

                return filteredList.Last().SpawnTableList;
            }
        }

        [System.Serializable]
        public class SpawnGroup
        {
            public EnemyMasterData SpawnTableList;
            public int Probability = 100;
            public bool IsOnlyOnce = false;
        }
    }
}