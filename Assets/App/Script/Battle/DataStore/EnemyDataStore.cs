using App.Battle.Data;
using App.Common.Data.MasterData;
using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    public class EnemyDataStore : IEnemyDataStore
    {
        private readonly EnemyDatabase _enemyDatabase;
        private readonly EnemySpawnDatabase _enemySpawnDatabase;

        private readonly Dictionary<int, EnemyData> _spawnEnemyDataList = new();

        private readonly Subject<int> _onEnemyAdded = new();
        public Observable<int> OnEnemyAdded => _onEnemyAdded;

        private readonly Subject<int> _onEnemyRemoved = new();
        public Observable<int> OnEnemyRemoved => _onEnemyRemoved;

        private readonly Subject<int> _onEnemyDead = new();
        public Observable<int> OnEnemyDead => _onEnemyDead;

        private readonly Subject<HitData> _onEnemyDeadByHit = new();
        public Observable<HitData> OnEnemyDeadByHit => _onEnemyDeadByHit;

        public List<EnemyData> Enemies => _spawnEnemyDataList.Values.ToList();

        private List<string> _onceSpawnedEnemyCodes = new();

        [Inject]
        public EnemyDataStore(
            EnemyDatabase enemyDatabase,
            EnemySpawnDatabase enemySpawnDatabase
        )
        {
            _enemyDatabase = enemyDatabase;
            _enemySpawnDatabase = enemySpawnDatabase;
        }

        private bool IsOnceSpawned(string enemyCode)
        {
            return _onceSpawnedEnemyCodes.Contains(enemyCode);
        }

        public bool TryGetEnemyMasterData(string enemyCode, out EnemyMasterData enemyMasterData)
        {
            enemyMasterData = null;
            if (!_enemyDatabase.TryGetEnemyMasterData(enemyCode, out var data))
            {
                return false;
            }

            enemyMasterData = data;
            return true;
        }

        public bool TryGetRandomEnemyMasterData(EnemyRankType rankType, UnlockCoreSkillType unlockCoreSkillType,
            out EnemyMasterData enemyMasterData)
        {
            const int MaxRetryCount = 100; // 最大試行回数
            int retryCount = 0;

            while (retryCount < MaxRetryCount)
            {
                retryCount++;

                enemyMasterData = null;
                if (!_enemySpawnDatabase.TryGetSpawnTable(unlockCoreSkillType, out var spawnTable))
                {
                    Debug.LogError("EnemySpawnDatabaseに該当するSpawnTableが存在しません");
                    return false;
                }

                var enemy = spawnTable.GetRandomSpawnEnemy(rankType);

                if (!_enemyDatabase.TryGetEnemyMasterData(enemy.SpawnEnemy.EnemyMasterDataId, out var enemyData))
                {
                    Debug.LogError(
                        $"EnemyDatabaseに該当するEnemyMasterDataが存在しません。EnemyMasterDataId:{enemy.SpawnEnemy.EnemyMasterDataId}");
                    return false;
                }

                if (!enemy.IsOnlyOnce)
                {
                    enemyMasterData = enemyData;
                    return true;
                }

                if (IsOnceSpawned(enemy.SpawnEnemy.EnemyMasterDataId))
                {
                    // すでに一度出現している場合は再度取得を試みる
                    continue;
                }

                _onceSpawnedEnemyCodes.Add(enemy.SpawnEnemy.EnemyMasterDataId);

                enemyMasterData = enemyData;
                return true;
            }

            // 最大試行回数を超えた場合（設定ミスの可能性）
            Debug.LogError($"敵のスポーンに失敗しました（最大試行回数超過）。スポーンテーブルの設定を確認してください。" +
                           $"UnlockType: {unlockCoreSkillType}, RankType: {rankType}。" +
                           $"IsOnlyOnce=falseの敵が最低1体は必要です。");
            enemyMasterData = null;
            return false;
        }

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

        public EnemyData AddEnemyData(EnemyMasterData enemyMasterData, Pose spawnPose)
        {
            int enemyId;
            do
            {
                enemyId = Random.Range(0, int.MaxValue);
            } while (_spawnEnemyDataList.ContainsKey(enemyId));

            var enemy = new EnemyData(enemyId, enemyMasterData, spawnPose);

            _spawnEnemyDataList.Add(enemyId, enemy);
            _onEnemyAdded.OnNext(enemyId);
            return enemy;
        }

        public bool RemoveEnemyData(int enemyId)
        {
            _onEnemyRemoved.OnNext(enemyId);
            return _spawnEnemyDataList.Remove(enemyId);
        }

        public void RemoveAllEnemyData()
        {
            // 辞書の列挙中に変更を避けるため一旦コピーしてから削除
            var ids = _spawnEnemyDataList.Keys.ToList();
            _spawnEnemyDataList.Clear();
            foreach (var id in ids)
            {
                _onEnemyRemoved.OnNext(id);
            }
        }

        public void Damage(HitData hitData)
        {
            if (!_spawnEnemyDataList.TryGetValue(hitData.DamagedId, out var enemyData))
            {
                return;
            }

            // 撃破済みの敵への追撃（マージの爆風や同フレームの別弾）は無視する。
            // 撃破演出の完了まで敵データが残るため、ガードが無いとOnEnemyDeadが二重発火する
            if (enemyData.IsDead)
            {
                return;
            }

            if (!TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var masterData))
            {
                return;
            }

            var damage = hitData.Damage;

            if (masterData.WeakDirectionType == hitData.HitDirectionType)
            {
                damage *= masterData.WeaknessMultiplier;
            }
            else if (masterData.ResistanceDirectionType == hitData.HitDirectionType)
            {
                damage *= masterData.ResistanceMultiplier;
            }

            //ダメージの最低保証は1
            if (damage <= 1f)
            {
                damage = 1;
            }

            enemyData.Hp -= damage;
            if (enemyData.Hp > 0)
            {
                return;
            }

            enemyData.IsDead = true;

            _onEnemyDead.OnNext(hitData.DamagedId);

            // 撃破の決め手となった命中情報（フォーム等）を参照したい購読者向けに、撃破と同時に流す
            _onEnemyDeadByHit.OnNext(hitData);
        }

        public void UpdateEnemyPose(int id, Pose pose)
        {
            if (!_spawnEnemyDataList.TryGetValue(id, out var enemyData))
            {
                return;
            }

            enemyData.Pose = pose;
        }
    }
}