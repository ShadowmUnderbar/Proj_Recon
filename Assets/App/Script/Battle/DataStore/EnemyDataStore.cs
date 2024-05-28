using System.Collections.Generic;
using App.Script.Battle.Data;
using App.Script.Battle.Interface.DataStore;
using UnityEngine;

namespace App.Script.Battle.DataStore
{
    public class EnemyDataStore : IEnemyDataStore
    {
        private readonly Dictionary<uint, EnemyData> _spawnEnemyDataList = new();

        public bool TryGetEnemyData(uint enemyId, out EnemyData enemyData)
        {
            enemyData = null;
            if (!_spawnEnemyDataList.TryGetValue(enemyId, out var data))
            {
                return false;
            }

            enemyData = data;
            return true;
        }

        public uint AddEnemyData(EnemyData enemyData)
        {
            uint enemyId = 0;
            do
            {
                enemyId = (uint)Random.Range(0, int.MaxValue);
            } while (_spawnEnemyDataList.ContainsKey(enemyId));

            _spawnEnemyDataList.Add(enemyId, enemyData);
            return enemyId;
        }

        public bool RemoveEnemyData(uint enemyId)
        {
            return _spawnEnemyDataList.Remove(enemyId);
        }
    }
}