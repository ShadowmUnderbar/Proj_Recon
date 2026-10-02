using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// デバッグ対戦（エディタの「App/デバッグ: 敵と対戦」から始めたバトル）で、指定の敵・ボスグループを出し、
    /// 全員いなくなったら待ち時間のあと同じ相手を出し直す。
    /// 配置はボスウェーブと同じ（プレイヤーは <see cref="BossWaveConfig.PlayerPosition"/>、相手は <see cref="BossWaveConfig.BossOrigin"/> 付近）。
    /// ウェーブの進行・周期スポーン・セット選択を止めるのは各 UseCase / DataStore が <see cref="DebugArenaSettings"/> を見て行う。
    /// </summary>
    public class DebugArenaUseCase : IInitializable, ITickable, IDisposable
    {
        // 通常の敵を複数出すときに散らす半径（m）
        private const float EnemyScatterRadius = 3f;

        private readonly DebugArenaSettings _settings;
        private readonly IDebugArenaDataStore _debugArenaDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBossGroupDataStore _bossGroupDataStore;
        private readonly IEnemyRandomSpawnCycleDataStore _enemyRandomSpawnCycleDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly BossWaveConfig _bossWaveConfig;

        private readonly CompositeDisposable _disposable = new();
        private readonly List<int> _spawnedIds = new();

        [Inject]
        public DebugArenaUseCase(
            DebugArenaSettings settings,
            IDebugArenaDataStore debugArenaDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IFreezeDataStore freezeDataStore,
            IEnemyDataStore enemyDataStore,
            IBossGroupDataStore bossGroupDataStore,
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IPlayerStateDataStore playerStateDataStore,
            BossWaveConfig bossWaveConfig
        )
        {
            _settings = settings;
            _debugArenaDataStore = debugArenaDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _freezeDataStore = freezeDataStore;
            _enemyDataStore = enemyDataStore;
            _bossGroupDataStore = bossGroupDataStore;
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _playerStateDataStore = playerStateDataStore;
            _bossWaveConfig = bossWaveConfig;
        }

        public void Initialize()
        {
            if (!_settings.IsEnabled)
            {
                return;
            }

            Debug.Log($"[DebugArenaUseCase] デバッグ対戦で開始します: {DescribeTarget()}"
                      + $"（出し直し: {(_settings.AutoRespawn ? $"{_settings.RespawnDelaySeconds}秒後" : "しない")}"
                      + $" / 無敵: {(_settings.Invincible ? "あり" : "なし")}）");

            // 撃破（OnEnemyDead のあと消去される）も、撃破扱いでない消去（体力共有の残り・出現失敗）も OnEnemyRemoved に来る。
            // 撃破を先に記録しておき、誰も撃破されずに消えたとき（読み込み失敗）は出し直さない
            _enemyDataStore.OnEnemyDead
                .Subscribe(_debugArenaDataStore.NotifyEnemyDead)
                .AddTo(_disposable);
            _enemyDataStore.OnEnemyRemoved
                .Subscribe(_debugArenaDataStore.NotifyEnemyRemoved)
                .AddTo(_disposable);
        }

        public void Tick()
        {
            if (!_settings.IsEnabled)
            {
                return;
            }

            // セット選択・ゲームオーバー・ショップ中（ウェーブ間ポーズ）とフリーズ中は出し直しの待ちも進めない
            if (_waveManagerDataStore.IsWavePause.Value || _freezeDataStore.IsFreezing.CurrentValue)
            {
                return;
            }

            _debugArenaDataStore.Tick(Time.deltaTime);

            if (_debugArenaDataStore.ShouldSpawn)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            if (_debugArenaDataStore.IsFirstSpawnInRun)
            {
                // ラン開始時だけ、ボスウェーブと同じ位置へプレイヤーを合わせる（出し直しのたびには動かさない）
                _enemyDataStore.RemoveAllEnemyData();
                _playerStateDataStore.WarpTo(_bossWaveConfig.PlayerPosition);
            }

            _spawnedIds.Clear();
            if (_settings.BossGroup != null)
            {
                _spawnedIds.AddRange(_bossGroupDataStore.SpawnGroup(_settings.BossGroup, _bossWaveConfig.BossOrigin));
            }
            else
            {
                SpawnEnemies(_spawnedIds);
            }

            if (_spawnedIds.Count == 0)
            {
                Debug.LogError($"[DebugArenaUseCase] 相手を出せませんでした。出し直しは行いません: {DescribeTarget()}");
            }

            _debugArenaDataStore.MarkSpawned(_spawnedIds);
        }

        private void SpawnEnemies(List<int> output)
        {
            if (!_enemyDataStore.TryGetEnemyMasterData(_settings.EnemyCode, out var masterData))
            {
                Debug.LogError($"[DebugArenaUseCase] EnemyMasterData が見つかりません: \"{_settings.EnemyCode}\"");
                return;
            }

            if (masterData.EnemyRankType == EnemyRankType.Boss)
            {
                // ボスの個体は台本の命令でしか動かないので、単体で出しても立っているだけになる
                Debug.LogWarning($"[DebugArenaUseCase] {masterData.EnemyMasterDataId} は Boss ランクです。"
                                 + "台本で動かすにはボスグループ（BossGroupConfig）を選んでください");
            }

            var origin = _bossWaveConfig.BossOrigin.position;
            var playerPosition = _playerStateDataStore.Position.Value;
            for (var i = 0; i < _settings.EnemyCount; i++)
            {
                // 1体なら基準点そのもの（半径0）。どちらも NavMesh 上の点に寄せる（外れると NavMeshAgent が置かれず動かない）
                var radius = _settings.EnemyCount == 1 ? 0f : EnemyScatterRadius;
                var position = _enemyRandomSpawnCycleDataStore.GetClusteredSpawnPositionFast(origin, radius);
                var enemy = _enemyDataStore.AddEnemyData(masterData, new Pose(position, LookAtFlat(position, playerPosition)));

                // プレハブの読み込みは無効なキーだと AddEnemyData の中で同期的に失敗し、敵データが取り除かれる。
                // 残っていない Id を相手に含めると、いなくなるのを待ち続けて出し直せなくなる
                if (_enemyDataStore.TryGetEnemyData(enemy.Id, out _))
                {
                    output.Add(enemy.Id);
                }
            }
        }

        private static Quaternion LookAtFlat(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
        }

        private string DescribeTarget()
        {
            return _settings.BossGroup != null
                ? $"ボスグループ {_settings.BossGroup.name}"
                : $"敵 {_settings.EnemyCode} ×{_settings.EnemyCount}";
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
