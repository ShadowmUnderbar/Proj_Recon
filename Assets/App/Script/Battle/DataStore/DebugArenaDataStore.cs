using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    public class DebugArenaDataStore : IDebugArenaDataStore, IRunResettable
    {
        private enum Phase
        {
            // ラン開始直後。最初の相手を出す
            NotSpawned,
            // 相手が残っている
            Fighting,
            // 全員いなくなり、出し直しを待っている
            WaitingRespawn,
            // 出し直さない（自動の出し直しが無効、または出現に失敗した）
            Finished
        }

        private readonly DebugArenaSettings _settings;
        // 残っている相手と、今回出した相手の全員（撃破の記録は消去の後に届くことがあるため、消えた相手も覚えておく）
        private readonly HashSet<int> _targetIds = new();
        private readonly HashSet<int> _spawnedIds = new();

        private Phase _phase = Phase.NotSpawned;
        private float _respawnRemainingSeconds;
        private bool _hasSpawnedInRun;
        private bool _isAnyDefeated;

        [Inject]
        public DebugArenaDataStore(DebugArenaSettings settings)
        {
            _settings = settings;
        }

        public bool IsFirstSpawnInRun => !_hasSpawnedInRun;

        public bool ShouldSpawn =>
            _phase == Phase.NotSpawned
            || (_phase == Phase.WaitingRespawn && _respawnRemainingSeconds <= 0f);

        public void MarkSpawned(IReadOnlyList<int> enemyIds)
        {
            _hasSpawnedInRun = true;
            _targetIds.Clear();
            _spawnedIds.Clear();
            foreach (var id in enemyIds)
            {
                _targetIds.Add(id);
                _spawnedIds.Add(id);
            }

            _isAnyDefeated = false;
            _phase = _targetIds.Count > 0 ? Phase.Fighting : Phase.Finished;
        }

        public void NotifyEnemyDead(int enemyId)
        {
            if (_phase == Phase.Fighting && _spawnedIds.Contains(enemyId))
            {
                _isAnyDefeated = true;
            }
        }

        public void NotifyEnemyRemoved(int enemyId)
        {
            if (_phase == Phase.Fighting)
            {
                _targetIds.Remove(enemyId);
            }
        }

        public void Tick(float deltaTime)
        {
            // 全員いなくなったかは次のフレームで判断する。撃破の通知の中で先に敵データが消される（BattleHitUseCase）ため、
            // 消去を受けた時点ではまだ撃破の記録が届いていないことがある
            if (_phase == Phase.Fighting && _targetIds.Count == 0)
            {
                if (_settings.AutoRespawn && _isAnyDefeated)
                {
                    _phase = Phase.WaitingRespawn;
                    _respawnRemainingSeconds = _settings.RespawnDelaySeconds;
                }
                else
                {
                    _phase = Phase.Finished;
                }

                return;
            }

            if (_phase == Phase.WaitingRespawn)
            {
                _respawnRemainingSeconds -= deltaTime;
            }
        }

        public void ResetRun()
        {
            _targetIds.Clear();
            _spawnedIds.Clear();
            _phase = Phase.NotSpawned;
            _respawnRemainingSeconds = 0f;
            _hasSpawnedInRun = false;
            _isAnyDefeated = false;
        }
    }
}
