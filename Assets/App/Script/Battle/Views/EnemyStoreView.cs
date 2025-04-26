using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace App.Battle.Views
{
    public class EnemyStoreView : MonoBehaviour, IEnemyStoreView
    {
        private Dictionary<int, IEnemyView> _enemies = new();

        private readonly Subject<(int id, Pose pose)> _onEnemyPoseUpdate = new();
        public Observable<(int id, Pose pose)> OnEnemyPoseUpdate => _onEnemyPoseUpdate;

        public async UniTask Spawn(EnemyData enemyData, Pose spawnPose)
        {
            var enemyObj = Addressables.LoadAssetAsync<GameObject>(enemyData.MasterData.PrefabPath);

            await enemyObj.Task;

            if (enemyObj.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            var view = Instantiate(enemyObj.Result, spawnPose.position, spawnPose.rotation)
                .GetComponent<IEnemyView>();

            view.Init(enemyData.Id);
            view.Pose.Subscribe(pose => _onEnemyPoseUpdate.OnNext((view.Id, pose)))
                .AddTo(this);

            _enemies.Add(enemyData.Id, view);
        }

        public async UniTask Dead(int id)
        {
            if (!_enemies.TryGetValue(id, out var enemyView))
            {
                return;
            }

            await enemyView.Dead();
            _enemies.Remove(id);
        }

        public void AllDeadEnemies()
        {
            foreach (var enemy in _enemies.Values)
            {
                enemy.Dead().Forget();
            }

            _enemies.Clear();
        }
    }
}