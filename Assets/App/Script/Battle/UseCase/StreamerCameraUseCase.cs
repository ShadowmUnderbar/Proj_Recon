using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 配信用カメラの演出トリガーを担う。
    /// ウェーブ進行・ボス出現／撃破・複数体同時撃破／同時命中を監視し、条件に応じて演出ショットを再生させる。
    /// </summary>
    public class StreamerCameraUseCase : IInitializable, ITickable, IDisposable
    {
        private static readonly IReadOnlyList<Vector3> NoSubject = Array.Empty<Vector3>();

        private readonly StreamerModeConfig _streamerModeConfig;
        private readonly StreamerCameraTriggerConfig _triggerConfig;
        private readonly IStreamerCameraDataStore _streamerCameraDataStore;
        private readonly IStreamerCameraPresenter _streamerCameraPresenter;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private readonly CompositeDisposable _disposable = new();

        private bool _isEnabled;

        [Inject]
        public StreamerCameraUseCase(
            StreamerModeConfig streamerModeConfig,
            StreamerCameraTriggerConfig triggerConfig,
            IStreamerCameraDataStore streamerCameraDataStore,
            IStreamerCameraPresenter streamerCameraPresenter,
            IEnemyDataStore enemyDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _streamerModeConfig = streamerModeConfig;
            _triggerConfig = triggerConfig;
            _streamerCameraDataStore = streamerCameraDataStore;
            _streamerCameraPresenter = streamerCameraPresenter;
            _enemyDataStore = enemyDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Initialize()
        {
            _isEnabled = _streamerModeConfig.IsEnabled
                         && (!_streamerModeConfig.VROnly || DebugConfig.IsVRMode);

            if (!_isEnabled)
            {
                return;
            }

            _waveManagerDataStore.OnWaveAdvanced
                .Subscribe(_ => OnWaveAdvanced())
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyAdded
                .Subscribe(OnEnemyAdded)
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyDead
                .Subscribe(OnEnemyDead)
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyDamaged
                .Subscribe(OnEnemyDamaged)
                .AddTo(_disposable);

            _streamerCameraPresenter.OnShotFinished
                .Subscribe(_ => _streamerCameraDataStore.EndShot())
                .AddTo(_disposable);
        }

        public void Tick()
        {
            if (!_isEnabled)
            {
                return;
            }

            _streamerCameraDataStore.AddElapsedTime(Time.deltaTime);
        }

        // ウェーブクリア→次ウェーブ開始の切り替わり。被写体はプレイヤー前方
        private void OnWaveAdvanced()
        {
            TryPlay(_triggerConfig.WaveAdvancedShot, NoSubject, "WaveAdvanced");
        }

        private void OnEnemyAdded(int enemyId)
        {
            if (!TryGetBossPosition(enemyId, out var position))
            {
                return;
            }

            TryPlay(_triggerConfig.BossSpawnShot, new[] { position }, "BossSpawn");
        }

        private void OnEnemyDead(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            _streamerCameraDataStore.RecordKill(enemyId, enemyData.Pose.position);

            // ボス撃破は複数体同時条件より優先して撮る
            if (IsBoss(enemyData.EnemyMasterDataId)
                && TryPlay(_triggerConfig.BossDeadShot, new[] { enemyData.Pose.position }, "BossDead"))
            {
                return;
            }

            EvaluateMultiTargetRules(StreamerCameraMultiTargetCountType.Kill);
        }

        private void OnEnemyDamaged(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            _streamerCameraDataStore.RecordDamage(enemyId, enemyData.Pose.position);

            EvaluateMultiTargetRules(StreamerCameraMultiTargetCountType.Damage);
        }

        // 上から順に判定し、最初に成立したルールだけを採用する
        private void EvaluateMultiTargetRules(StreamerCameraMultiTargetCountType countType)
        {
            foreach (var rule in _triggerConfig.MultiTargetRules)
            {
                if (rule == null || !rule.IsValid || rule.CountType != countType)
                {
                    continue;
                }

                if (!_streamerCameraDataStore.TryCollectMultiTargetPositions(rule, out var subjectPositions))
                {
                    continue;
                }

                if (!TryPlay(rule.ShotData, subjectPositions, rule.RuleName))
                {
                    continue;
                }

                // 発動できたら履歴を空にして、直後の1体追加で再発動しないようにする
                _streamerCameraDataStore.ClearMultiTargetHistory(countType);
                return;
            }
        }

        private bool TryPlay(StreamerCameraShotData shotData, IReadOnlyList<Vector3> subjectPositions, string triggerName)
        {
            if (shotData == null || !_streamerCameraDataStore.CanPlay(shotData))
            {
                return false;
            }

            _streamerCameraDataStore.BeginShot(shotData);
            _streamerCameraPresenter.PlayShot(new StreamerCameraShotRequest(shotData, subjectPositions, triggerName));
            return true;
        }

        private bool TryGetBossPosition(int enemyId, out Vector3 position)
        {
            position = default;

            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return false;
            }

            if (!IsBoss(enemyData.EnemyMasterDataId))
            {
                return false;
            }

            position = enemyData.Pose.position;
            return true;
        }

        private bool IsBoss(string enemyMasterDataId)
        {
            return _enemyDataStore.TryGetEnemyMasterData(enemyMasterDataId, out var masterData)
                   && _triggerConfig.IsBossRank(masterData.EnemyRankType);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
