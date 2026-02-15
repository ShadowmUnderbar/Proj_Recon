using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
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
        private IBattlePlayerView _playerView;
        private IHitBoxStoreView _hitBoxStoreView;

        private readonly Dictionary<int, IEnemyView> _enemies = new();

        private readonly Subject<(int id, Pose pose)> _onEnemyPoseUpdate = new();
        public Observable<(int id, Pose pose)> OnEnemyPoseUpdate => _onEnemyPoseUpdate;

        private readonly RaycastHit[] _hits = new RaycastHit[10];
        private readonly List<int> _rayCastEnemyIds = new();

        [Inject]
        public void Construct(
            IHitBoxStoreView hitBoxStoreView,
            IBattlePlayerView playerView
        )
        {
            _hitBoxStoreView = hitBoxStoreView;
            _playerView = playerView;
        }

        public async UniTask Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType)
        {
            var enemyObj = Addressables.LoadAssetAsync<GameObject>(prefabPath);

            await enemyObj.Task;

            if (enemyObj.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            var view = Instantiate(enemyObj.Result, enemyData.Pose.position, enemyData.Pose.rotation)
                .GetComponent<IEnemyView>();

            view.Init(enemyData.Id, enemyData, resistanceDirectionType);
            view.Pose
                .Subscribe(x => _onEnemyPoseUpdate.OnNext((view.Id, x)))
                .AddTo(view as MonoBehaviour);
            view.SetPlayerTransform(_playerView.PlayerTransform);

            _enemies.Add(enemyData.Id, view);

            foreach (var hitBox in view.HitBoxes)
            {
                _hitBoxStoreView.AddHitBoxView(hitBox);
            }
        }

        public void UnSpawn(int enemyId)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemyView))
            {
                return;
            }

            _hitBoxStoreView.RemoveHitBoxView(enemyId);

            enemyView.Destroy();
            _enemies.Remove(enemyId);
        }

        public async UniTask Dead(int id)
        {
            if (!_enemies.TryGetValue(id, out var enemyView))
            {
                return;
            }

            await enemyView.Dead();
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

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            foreach (var enemy in _enemies.Values)
            {
                enemy.SetPlayerAimDirection(aimDir1, aimDir2);
            }
        }

        private void OnDestroy()
        {
            foreach (var enemy in _enemies.Values)
            {
                enemy.Destroy();
            }

            _enemies.Clear();
            _onEnemyPoseUpdate.Dispose();
        }
    }
}