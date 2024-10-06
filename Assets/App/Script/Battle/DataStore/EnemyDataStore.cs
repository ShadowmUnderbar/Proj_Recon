using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data.MasterData;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.DataStore
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

        public EnemyData AddEnemyData(EnemyMasterData enemyMasterData)
        {
            uint enemyId;
            do
            {
                enemyId = (uint)Random.Range(0, int.MaxValue);
            } while (_spawnEnemyDataList.ContainsKey(enemyId));

            var enemy = new EnemyData
            {
                Id = enemyId,
                MasterData = enemyMasterData,
                Hp = enemyMasterData.Hp,
                Damage = enemyMasterData.Damage,
                Speed = enemyMasterData.Speed,
            };

            _spawnEnemyDataList.Add(enemyId, enemy);
            return enemy;
        }

        public bool RemoveEnemyData(uint enemyId)
        {
            return _spawnEnemyDataList.Remove(enemyId);
        }
    }
}