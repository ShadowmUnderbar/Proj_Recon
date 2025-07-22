using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

namespace App.Battle.Views
{
    public class EnemyStoreView : MonoBehaviour, IEnemyStoreView
    {
        private IHitBoxStoreView _hitBoxStoreView;

        private readonly Dictionary<int, IEnemyView> _enemies = new();

        private readonly Subject<(int id, Pose pose)> _onEnemyPoseUpdate = new();
        public Observable<(int id, Pose pose)> OnEnemyPoseUpdate => _onEnemyPoseUpdate;

        private readonly RaycastHit[] _hits = new RaycastHit[10];
        private readonly List<int> _rayCastEnemyIds = new();

        [Inject]
        public void Construct(
            IHitBoxStoreView hitBoxStoreView
        )
        {
            _hitBoxStoreView = hitBoxStoreView;
        }

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

            view.Init(enemyData.Id, enemyData);
            view.Pose.Subscribe(pose => _onEnemyPoseUpdate.OnNext((view.Id, pose)))
                .AddTo(this);

            _enemies.Add(enemyData.Id, view);

            foreach (var hitBox in view.HitBoxes)
            {
                _hitBoxStoreView.AddHitBoxView(hitBox);
            }
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

        public int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance)
        {
            var count = Physics.SphereCastNonAlloc(playerPosition, 0.5f, direction.normalized, _hits, distance,
                LayerMasks.EnemyLayer);

            if (count <= 0)
            {
                return null;
            }

            _rayCastEnemyIds.Clear();

            for (var i = 0; i < count; i++)
            {
                var enemy = _hits[i].collider.GetComponent<HitBoxView>();
                if (enemy == null)
                {
                    continue;
                }

                _rayCastEnemyIds.Add(enemy.Id);
            }

            return _rayCastEnemyIds.ToArray();
        }
    }
}