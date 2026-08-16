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

        // 注視判定用（毎フレーム呼ばれるためバッファを使い回す）
        private readonly RaycastHit[] _gazeHits = new RaycastHit[20];
        private readonly List<int> _gazeEnemyIds = new();

        private bool _isPause;

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
            // 非同期ロード中にポーズ状態が変わっていても現在の状態を反映する
            view.SetPause(_isPause);

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

        public IReadOnlyList<int> GetGazeEnemies(Vector3 origin, Vector3 direction, float radius, float distance)
        {
            _gazeEnemyIds.Clear();

            var count = Physics.SphereCastNonAlloc(origin, radius, direction.normalized, _gazeHits, distance,
                LayerMasks.EnemyLayer);

            for (var i = 0; i < count; i++)
            {
                var hitBox = _gazeHits[i].collider.GetComponent<HitBoxView>();
                if (hitBox == null)
                {
                    continue;
                }

                // 1体の敵が複数のヒットボックスを持つため重複を除く
                if (_gazeEnemyIds.Contains(hitBox.Id))
                {
                    continue;
                }

                _gazeEnemyIds.Add(hitBox.Id);
            }

            return _gazeEnemyIds;
        }

        public void SetSpeedMultiplier(int enemyId, float multiplier)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemyView))
            {
                return;
            }

            enemyView.SetSpeedMultiplier(multiplier);
        }

        public void SetStun(int enemyId, bool isStun)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemyView))
            {
                return;
            }

            enemyView.SetStun(isStun);
        }

        public void PlayHitFeedback(int enemyId, Vector3 hitDirection)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemyView))
            {
                return;
            }

            enemyView.PlayHitFeedback(hitDirection);
        }

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            foreach (var enemy in _enemies.Values)
            {
                enemy.SetPlayerAimDirection(aimDir1, aimDir2);
            }
        }

        public void SetPause(bool isPause)
        {
            _isPause = isPause;

            foreach (var enemy in _enemies.Values)
            {
                enemy.SetPause(isPause);
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