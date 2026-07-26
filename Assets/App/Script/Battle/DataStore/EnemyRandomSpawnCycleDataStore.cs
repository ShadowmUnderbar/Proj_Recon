using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class EnemyRandomSpawnCycleDataStore : IEnemyRandomSpawnCycleDataStore, ITickable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        [Inject]
        public EnemyRandomSpawnCycleDataStore(IWaveManagerDataStore waveManagerDataStore)
        {
            _waveManagerDataStore = waveManagerDataStore;
        }

        private readonly Subject<int> _onSpawnCommonEnemy = new();
        public Observable<int> OnSpawnCommonEnemy => _onSpawnCommonEnemy;

        private readonly Subject<int> _onSpawnMinorEnemy = new();
        public Observable<int> OnSpawnMinorEnemy => _onSpawnMinorEnemy;

        private readonly Subject<Unit> _onSpawnMajorEnemy = new();
        public Observable<Unit> OnSpawnMajorEnemy => _onSpawnMajorEnemy;

        private readonly Subject<Unit> _onSpawnBossEnemy = new();
        public Observable<Unit> OnSpawnBossEnemy => _onSpawnBossEnemy;

        private readonly Subject<Unit> _onSpawnIrregularEnemy = new();
        public Observable<Unit> OnSpawnIrregularEnemy => _onSpawnIrregularEnemy;

        private float SpawnDistanceMin => 25f;
        private float SpawnDistanceMax => 40f;

        private float _commonSpawnCycle;
        private float CommonSpawnInterval => 4f;
        private int _commonSpawnCounts = 2;

        private float CommonSpawnCountUpInterval => 40f;
        private float _commonSpawnCountUpCycle;

        private float _minorSpawnCycle;
        private float MinorSpawnInterval => 20f;
        private float _minorSpawnCounts = 1;

        private float _majorSpawnCycle;
        private float MajorSpawnInterval => 50f;

        public void Tick()
        {
            // ウェーブ間ポーズ中はスポーンタイマーを進めない（敵の生成を停止）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _commonSpawnCycle += Time.deltaTime;
            _minorSpawnCycle += Time.deltaTime;
            _majorSpawnCycle += Time.deltaTime;
            _commonSpawnCountUpCycle += Time.deltaTime;

            if (_commonSpawnCycle >= CommonSpawnInterval)
            {
                _commonSpawnCycle = 0f;
                var count = Random.Range(_commonSpawnCounts, _commonSpawnCounts * 1.5f);
                _onSpawnCommonEnemy.OnNext(Mathf.FloorToInt(count));
            }

            if (_commonSpawnCountUpCycle >= CommonSpawnCountUpInterval)
            {
                _commonSpawnCountUpCycle = 0f;
                _commonSpawnCounts++;
            }

            if (_minorSpawnCycle >= MinorSpawnInterval)
            {
                _minorSpawnCycle = 0f;

                _onSpawnMinorEnemy.OnNext(Mathf.FloorToInt(_minorSpawnCounts));
            }

            if (_majorSpawnCycle >= MajorSpawnInterval)
            {
                _majorSpawnCycle = 0f;
                _minorSpawnCounts *= 1.5f;
                _onSpawnMajorEnemy.OnNext(Unit.Default);
            }
        }

        public void ResetSpawnCycle()
        {
            // 累積スポーンタイマーのみゼロ化（_commonSpawnCounts等の難易度カウントはウェーブを跨いで維持）
            _commonSpawnCycle = 0f;
            _minorSpawnCycle = 0f;
            _majorSpawnCycle = 0f;
            _commonSpawnCountUpCycle = 0f;
        }

        // NavMeshサンプルの最大試行回数。
        // 上限なしでループすると、プレイヤーがマップ端に居てスポーン円環(25〜40m)全体が
        // NavMesh外になった場合に永遠に成功せず、メインスレッドが完全フリーズする（実際に発生した不具合）
        private const int MaxSampleRetryCount = 30;

        public Vector3 GetRandomSpawnPositionFast(Vector3 playerPosition)
        {
            for (var i = 0; i < MaxSampleRetryCount; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                var distance = Random.Range(SpawnDistanceMin, SpawnDistanceMax);
                var targetPos = playerPosition + dir * distance;

                // 生のtargetPosは最大1mメッシュ外にズレうるため、必ずNavMesh上の点を返す
                // （メッシュ外にスポーンするとNavMeshAgentが配置されず、動けない敵になる）
                if (NavMesh.SamplePosition(targetPos, out var hit, 1.0f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // 円環全体がNavMesh外の場合のフォールバック: プレイヤー周辺の最寄りNavMesh上の点を返す
            if (NavMesh.SamplePosition(playerPosition, out var fallbackHit, SpawnDistanceMax, NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }

            Debug.LogWarning("[EnemyRandomSpawnCycleDataStore] スポーン位置のNavMeshサンプルに失敗したため、プレイヤー位置にフォールバックします");
            return playerPosition;
        }

        public Vector3 GetClusteredSpawnPositionFast(Vector3 origin, float radius)
        {
            for (var i = 0; i < MaxSampleRetryCount; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                var distance = Random.Range(0f, radius);
                var targetPos = origin + dir * distance;

                if (NavMesh.SamplePosition(targetPos, out var hit, 1.0f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // 半径内が全てNavMesh外の場合は origin をそのまま返す（origin は直前に採用済みのNavMesh上の点）
            return origin;
        }
                // 陽動の方向寄せで、指定方向にどれだけ角度の散らばりを許すか（±の振れ幅）
        private float DirectionalSpawnJitterRad => 20f * Mathf.Deg2Rad;

        public Vector3 GetDirectionalSpawnPositionFast(Vector3 playerPosition, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                // 方向が定まらない場合は通常のランダムスポーンに委ねる
                return GetRandomSpawnPositionFast(playerPosition);
            }

            var baseAngle = Mathf.Atan2(direction.z, direction.x);
            for (var i = 0; i < MaxSampleRetryCount; i++)
            {
                var angle = baseAngle + Random.Range(-DirectionalSpawnJitterRad, DirectionalSpawnJitterRad);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                var distance = Random.Range(SpawnDistanceMin, SpawnDistanceMax);
                var targetPos = playerPosition + dir * distance;

                if (NavMesh.SamplePosition(targetPos, out var hit, 1.0f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // 指定方向がNavMesh外だった場合は通常のフォールバックに合わせる
            if (NavMesh.SamplePosition(playerPosition, out var fallbackHit, SpawnDistanceMax, NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }

            Debug.LogWarning("[EnemyRandomSpawnCycleDataStore] 方向指定スポーン位置のNavMeshサンプルに失敗したため、プレイヤー位置にフォールバックします");
            return playerPosition;
        }
    }
}