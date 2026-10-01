using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
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
        private EnemyGazeDebugSettings _gazeDebugSettings;

        private readonly Dictionary<int, IEnemyView> _enemies = new();

        private readonly Subject<(int id, Pose pose)> _onEnemyPoseUpdate = new();
        public Observable<(int id, Pose pose)> OnEnemyPoseUpdate => _onEnemyPoseUpdate;

        // ボスグループの個体（台本の命令を受けるAIを持つ敵だけ）
        private readonly Dictionary<int, IBossMemberView> _bossMembers = new();
        private readonly Subject<(int id, BossMemberStatus status)> _onBossMemberStatusChanged = new();
        public Observable<(int id, BossMemberStatus status)> OnBossMemberStatusChanged => _onBossMemberStatusChanged;

        private readonly RaycastHit[] _hits = new RaycastHit[10];
        private readonly List<int> _rayCastEnemyIds = new();

        [Header("注視判定（スネークアイズ・メデューサ・ガン飛ばし）")]
        [SerializeField, Min(1), Tooltip("1フレームに視線との距離を更新する敵の数。敵が多くて反応が遅く感じたら増やす")]
        private int _gazeEnemiesPerFrame = 3;

        [SerializeField, Min(0f), Tooltip("視線に捉えたとみなす最大距離[m]")]
        private float _gazeMaxDistance = 50f;

        // 敵ごとの判定球と視線の距離を順番に更新する
        private readonly EnemyGazeTracker _gazeTracker = new();

        // デバッグ: 視線が判定球を通っている敵（通った瞬間だけリアクションを出すため、前回の状態を持つ）
        private readonly HashSet<int> _gazeTouchedEnemyIds = new();

        // 直線判定用（注視と同時に呼ばれてもバッファが混ざらないよう別に持つ）
        private readonly RaycastHit[] _lineHits = new RaycastHit[32];
        private readonly List<int> _lineEnemyIds = new();

        private bool _isPause;

        // 回避の通過判定に使う SphereCast の半径（m）
        private const float DodgeHitRadius = 0.5f;

        [Inject]
        public void Construct(
            IHitBoxStoreView hitBoxStoreView,
            IBattlePlayerView playerView,
            EnemyGazeDebugSettings gazeDebugSettings
        )
        {
            _hitBoxStoreView = hitBoxStoreView;
            _playerView = playerView;
            _gazeDebugSettings = gazeDebugSettings;
        }

        public async UniTask Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType)
        {
            var enemyObj = Addressables.LoadAssetAsync<GameObject>(prefabPath);

            await enemyObj.Task;

            if (enemyObj.Status != AsyncOperationStatus.Succeeded)
            {
                // 敵データだけ残り倒せない敵になる（ボスウェーブでは進行不能になる）ため、気付けるようにする
                Debug.LogError($"[{nameof(EnemyStoreView)}] 敵プレハブの読み込みに失敗しました: {prefabPath}（敵Id: {enemyData.Id}）");
                return;
            }

            var enemyObject = Instantiate(enemyObj.Result, enemyData.Pose.position, enemyData.Pose.rotation);
            var view = enemyObject.GetComponent<IEnemyView>();

            view.Init(enemyData.Id, enemyData, resistanceDirectionType);
            view.Pose
                .Subscribe(x => _onEnemyPoseUpdate.OnNext((view.Id, x)))
                .AddTo(view as MonoBehaviour);
            view.SetPlayerTransform(_playerView.PlayerTransform);
            // 非同期ロード中にポーズ状態が変わっていても現在の状態を反映する
            view.SetPause(_isPause);

            _enemies.Add(enemyData.Id, view);

            if (enemyObject.TryGetComponent<IBossMemberView>(out var bossMember))
            {
                var enemyId = enemyData.Id;
                _bossMembers.Add(enemyId, bossMember);
                bossMember.Status
                    .Subscribe(status => _onBossMemberStatusChanged.OnNext((enemyId, status)))
                    .AddTo(enemyObject);
            }

            if (enemyObject.TryGetComponent<EnemyGazeBoundsView>(out var gazeBounds))
            {
                _gazeTracker.Add(enemyData.Id, gazeBounds);
            }
            else
            {
                Debug.LogWarning($"[{nameof(EnemyStoreView)}] {enemyObject.name}: {nameof(EnemyGazeBoundsView)} が無いため注視系アップグレードの対象になりません", enemyObject);
            }

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
            _gazeTracker.Remove(enemyId);
            _gazeTouchedEnemyIds.Remove(enemyId);
            _bossMembers.Remove(enemyId);

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
            _gazeTracker.Clear();
            _gazeTouchedEnemyIds.Clear();
            _bossMembers.Clear();
        }

        public IReadOnlyList<int> GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance)
        {
            _rayCastEnemyIds.Clear();

            var count = Physics.SphereCastNonAlloc(playerPosition, DodgeHitRadius, direction.normalized, _hits, distance,
                LayerConstants.Enemy);

            if (count <= 0)
            {
                return _rayCastEnemyIds;
            }

            for (var i = 0; i < count; i++)
            {
                var enemy = _hits[i].collider.GetComponent<HitBoxView>();
                if (enemy == null)
                {
                    continue;
                }

                _rayCastEnemyIds.Add(enemy.Id);
            }

            return _rayCastEnemyIds;
        }

        public IReadOnlyList<(int enemyId, float distanceFromRay)> GetGazeEnemyDistances()
        {
            return _gazeTracker.Distances;
        }

        private void LateUpdate()
        {
            // 頭の姿勢はトラッキング更新後の LateUpdate で取る。取れないフレームは前回の距離を据え置く
            if (_playerView == null || !_playerView.TryGetGazePose(out var gazePose))
            {
                return;
            }

            _gazeTracker.EvaluateNext(gazePose, _gazeEnemiesPerFrame, _gazeMaxDistance);

            if (_gazeDebugSettings != null && _gazeDebugSettings.PlayHitFeedbackOnGazeTouch)
            {
                PlayGazeTouchHitFeedback(gazePose.position);
            }
        }

        /// <summary>
        /// デバッグ: 視線が判定球を通った（距離が0になった）瞬間の敵に被弾リアクションを出す。
        /// 通り続けている間は出さず、一度外れてから再び通ったらまた出す
        /// </summary>
        private void PlayGazeTouchHitFeedback(Vector3 gazeOrigin)
        {
            var distances = _gazeTracker.Distances;
            for (var i = 0; i < distances.Count; i++)
            {
                var (enemyId, distanceFromRay) = distances[i];

                if (distanceFromRay > 0f)
                {
                    _gazeTouchedEnemyIds.Remove(enemyId);
                    continue;
                }

                if (!_gazeTouchedEnemyIds.Add(enemyId))
                {
                    continue;
                }

                if (!_enemies.TryGetValue(enemyId, out var enemyView))
                {
                    continue;
                }

                // 視線の出どころ→敵の水平方向へ傾ける
                var hitDirection = enemyView.Pose.Value.position - gazeOrigin;
                hitDirection.y = 0f;
                enemyView.PlayHitFeedback(hitDirection.sqrMagnitude > 0f ? hitDirection.normalized : Vector3.zero);
            }
        }

        public IReadOnlyList<int> GetLineHitEnemies(Vector3 origin, Vector3 direction, float radius, float distance)
        {
            return SphereCastEnemyIds(origin, direction, radius, distance, _lineHits, _lineEnemyIds);
        }

        /// <summary>
        /// 指定の球を direction 方向へ distance だけ飛ばし、当たった敵のIdを results に詰めて返す。
        /// バッファは呼び出しごとに使い回すため、戻り値は次の呼び出しまでに使い切る。
        /// </summary>
        private static IReadOnlyList<int> SphereCastEnemyIds(Vector3 origin, Vector3 direction, float radius,
            float distance, RaycastHit[] hits, List<int> results)
        {
            results.Clear();

            var count = Physics.SphereCastNonAlloc(origin, radius, direction.normalized, hits, distance,
                LayerConstants.Enemy);

            for (var i = 0; i < count; i++)
            {
                var hitBox = hits[i].collider.GetComponent<HitBoxView>();
                if (hitBox == null)
                {
                    continue;
                }

                // 1体の敵が複数のヒットボックスを持つため重複を除く
                if (results.Contains(hitBox.Id))
                {
                    continue;
                }

                results.Add(hitBox.Id);
            }

            return results;
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

        public void KnockBack(int enemyId, Vector3 destination, float duration)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemyView))
            {
                return;
            }

            enemyView.KnockBack(destination, duration);
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

        public void CommandBossAction(int enemyId, int actionIndex)
        {
            if (!_bossMembers.TryGetValue(enemyId, out var bossMember))
            {
                return;
            }

            bossMember.CommandAction(actionIndex);
        }

        public void SetBossHold(int enemyId, bool isHold)
        {
            if (!_bossMembers.TryGetValue(enemyId, out var bossMember))
            {
                return;
            }

            bossMember.SetHold(isHold);
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
                // シーン破棄では敵が先に破棄されていることがある（破棄済みに Destroy を呼ぶと例外になる）
                if (enemy is Object enemyObject && enemyObject == null)
                {
                    continue;
                }

                enemy.Destroy();
            }

            _enemies.Clear();
            _gazeTracker.Clear();
            _gazeTouchedEnemyIds.Clear();
            _bossMembers.Clear();
            _onEnemyPoseUpdate.Dispose();
            _onBossMemberStatusChanged.Dispose();
        }
    }
}