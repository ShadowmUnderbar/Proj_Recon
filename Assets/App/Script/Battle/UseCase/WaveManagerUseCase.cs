using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class WaveManagerUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyRandomSpawnCycleDataStore _enemyRandomSpawnCycleDataStore;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly WaveConfig _waveConfig;
        private readonly IFreezeDataStore _freezeDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public WaveManagerUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IBulletStoreView bulletStoreView,
            WaveConfig waveConfig,
            IFreezeDataStore freezeDataStore
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _bulletStoreView = bulletStoreView;
            _waveConfig = waveConfig;
            _freezeDataStore = freezeDataStore;
        }

        public void Initialize()
        {
            // ポーズ状態を敵側へ伝搬
            _waveManagerDataStore.IsWavePause
                .Subscribe(OnUpdateWavePause)
                .AddTo(_disposable);

            // 敵撃破でキル数加算 → 進行条件評価
            _enemyDataStore.OnEnemyDead
                .Subscribe(_ => OnEnemyDead())
                .AddTo(_disposable);
        }

        public void Tick()
        {
            // ポーズ中は経過時間を進めない（ウェーブ遷移中・将来のウェーブ選択UI中の停止）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _waveManagerDataStore.AddElapsedTime(Time.deltaTime);
            TryAdvanceWave();
        }

        private void OnEnemyDead()
        {
            // ポーズ中はカウントしない（ウェーブ遷移中の死亡通知をスキップ）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _waveManagerDataStore.IncrementKillCount();
            TryAdvanceWave();
        }

        private void TryAdvanceWave()
        {
            // 最大ウェーブ到達時は進行しない（無限ループ設定なら HasMaxWave=false でスキップ）
            if (_waveConfig.HasMaxWave
                && _waveManagerDataStore.CurrentWave.CurrentValue >= _waveConfig.MaxWaveCount)
            {
                return;
            }

            var isTimeReached = _waveManagerDataStore.ElapsedTime.CurrentValue
                                >= _waveConfig.WaveDurationSeconds;
            var isKillCountReached = _waveManagerDataStore.KillCount.CurrentValue
                                     >= _waveConfig.WaveEnemyKillCount;

            if (!isTimeReached && !isKillCountReached)
            {
                return;
            }

            AdvanceWaveInternal();
        }

        private void AdvanceWaveInternal()
        {
            // 敵の停止＋無敵化（ショップの「次のウェーブへ」で解除）
            // 敵は消さずに残し、ウェーブ再開時に動きを再開させる
            _waveManagerDataStore.SetWavePause(true);
            // スポーン累積タイマーをリセットして次ウェーブの初期間隔から再開
            _enemyRandomSpawnCycleDataStore.ResetSpawnCycle();
            // プレイヤー弾・敵弾を全消去
            _bulletStoreView.AllRemove();
            // ウェーブ番号インクリメント＋進行通知
            _waveManagerDataStore.AdvanceWave();
        }

        private void OnUpdateWavePause(bool isPause)
        {
            // 敵の停止はフリーズと共有の機構なので、フリーズ中の解除で動き出さないよう論理和で渡す
            _enemyPresenter.SetPause(isPause || _freezeDataStore.IsFreezing.CurrentValue);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
