using System.Collections.Generic;
using App.Script.Battle.Data;
using App.Script.Battle.Interface.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace App.Script.Battle.Views
{
    public class EnemyStoreView : MonoBehaviour, IEnemyStoreView
    {
        private Dictionary<uint, IEnemyView> _enemies = new();

        public async UniTask Spawn(EnemyData enemyData, Pose spawnPose)
        {
            var enemyobj = Addressables.LoadAssetAsync<GameObject>(enemyData.MasterData.PrefabPath);

            await enemyobj.Task;

            if (enemyobj.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            var view = Instantiate(enemyobj.Result, spawnPose.position, spawnPose.rotation)
                .GetComponent<IEnemyView>();

            _enemies.Add(enemyData.Id, view);
        }
    }
}