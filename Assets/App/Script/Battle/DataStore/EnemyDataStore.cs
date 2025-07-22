using App.Battle.Data;
using App.Common.Data.MasterData;
using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using UnityEngine;

namespace App.Battle.DataStore
{
    public class EnemyDataStore : IEnemyDataStore
    {
        private readonly Dictionary<int, EnemyData> _spawnEnemyDataList = new();

        public bool TryGetEnemyData(int enemyId, out EnemyData enemyData)
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
            int enemyId;
            do
            {
                enemyId = Random.Range(0, int.MaxValue);
            } while (_spawnEnemyDataList.ContainsKey(enemyId));

            var enemy = new EnemyData
            {
                Id = enemyId,
                MasterData = enemyMasterData,
                Hp = enemyMasterData.Hp,
                Damage = enemyMasterData.Damage,
                AttackInterval = enemyMasterData.AttackInterval,
                IdleSpeed = enemyMasterData.IdleSpeed,
                BattleSpeed = enemyMasterData.BattleSpeed,
            };

            _spawnEnemyDataList.Add(enemyId, enemy);
            return enemy;
        }

        public bool RemoveEnemyData(int enemyId)
        {
            return _spawnEnemyDataList.Remove(enemyId);
        }

        public void Damage(int enemyId, float damage)
        {
            if (!_spawnEnemyDataList.TryGetValue(enemyId, out var enemyData))
            {
                return;
            }

            Debug.Log("ヒット：" + enemyId + " " + damage);

            enemyData.Hp -= damage;
            if (enemyData.Hp <= 0)
            {
                RemoveEnemyData(enemyId);
            }
        }
    }
}