using App.Battle.Data;
using App.Common.Data.MasterData;
using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;

namespace App.Battle.DataStore
{
    public class EnemyDataStore : IEnemyDataStore
    {
        private readonly Dictionary<int, EnemyData> _spawnEnemyDataList = new();

        private readonly Subject<int> _onEnemyAdded = new();
        public Observable<int> OnEnemyAdded => _onEnemyAdded;

        private readonly Subject<int> _onEnemyRemoved = new();
        public Observable<int> OnEnemyRemoved => _onEnemyRemoved;

        private readonly Subject<int> _onEnemyDead = new();
        public Observable<int> OnEnemyDead => _onEnemyDead;

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
                FindDistanceRange = enemyMasterData.FindDistance,
                BattleSpeed = enemyMasterData.BattleSpeed,
            };

            _spawnEnemyDataList.Add(enemyId, enemy);
            _onEnemyAdded.OnNext(enemyId);
            return enemy;
        }

        public bool RemoveEnemyData(int enemyId)
        {
            _onEnemyRemoved.OnNext(enemyId);
            return _spawnEnemyDataList.Remove(enemyId);
        }

        public void Damage(int enemyId, float damage)
        {
            if (!_spawnEnemyDataList.TryGetValue(enemyId, out var enemyData))
            {
                return;
            }

            enemyData.Hp -= damage;
            if (enemyData.Hp > 0)
            {
                return;
            }

            _onEnemyDead.OnNext(enemyId);
        }
    }
}