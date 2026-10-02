using System;
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
    public class WaveManagerUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyRandomSpawnCycleDataStore _enemyRandomSpawnCycleDataStore;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly IPointParticlePresenter _pointParticlePresenter;
        private readonly WaveConfig _waveConfig;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IBossWaveDataStore _bossWaveDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly DebugArenaSettings _debugArenaSettings;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public WaveManagerUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IBulletStoreView bulletStoreView,
            IPointParticlePresenter pointParticlePresenter,
            WaveConfig waveConfig,
            IFreezeDataStore freezeDataStore,
            IBossWaveDataStore bossWaveDataStore,
            IGameStateDataStore gameStateDataStore,
            DebugArenaSettings debugArenaSettings
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _bulletStoreView = bulletStoreView;
            _pointParticlePresenter = pointParticlePresenter;
            _waveConfig = waveConfig;
            _freezeDataStore = freezeDataStore;
            _bossWaveDataStore = bossWaveDataStore;
            _gameStateDataStore = gameStateDataStore;
            _debugArenaSettings = debugArenaSettings;
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
            // デバッグ対戦は同じ相手と戦い続けるため、ウェーブを進めない（ショップも開かない）
            if (_debugArenaSettings.IsEnabled)
            {
                return;
            }

            // ゲームオーバー・クリア後は進めない
            if (_gameStateDataStore.IsRunEnded)
            {
                return;
            }

            // 最大ウェーブ到達時は進行しない（無限ループ設定なら HasMaxWave=false でスキップ）。
            // ボスウェーブのクリアは進行ではないので、最大ウェーブでも行う
            var isMaxWaveReached = _waveConfig.HasMaxWave
                                   && _waveManagerDataStore.CurrentWave.CurrentValue >= _waveConfig.MaxWaveCount;

            // ボスウェーブは制限時間・撃破数では進めず、ボスを全員倒したらクリアにする。
            // 出現・読み込みの失敗で誰も倒さずに消えたときはクリアにせず、止まらないよう次のウェーブへ進める
            if (_bossWaveDataStore.IsBossWave)
            {
                if (_bossWaveDataStore.IsBossDefeated)
                {
                    ClearRunInternal();
                }
                else if (_bossWaveDataStore.IsBossCleared && !isMaxWaveReached)
                {
                    AdvanceWaveInternal();
                }

                return;
            }

            if (isMaxWaveReached)
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
            // 漂っているポイント粒子も全消去（未回収のぶんはウェーブ跨ぎで持ち越さない）
            _pointParticlePresenter.AllRemove();
            // ウェーブ番号インクリメント＋進行通知
            _waveManagerDataStore.AdvanceWave();
        }

        private void ClearRunInternal()
        {
            // 敵の停止＋無敵化。ウェーブ番号は進めず（スロットにはボスウェーブの番号を残す）、ショップも開かない
            _waveManagerDataStore.SetWavePause(true);
            // クリア表示の間に撃たれないよう弾を消す。粒子はラン終了で回収の意味が無くなるので消す
            _bulletStoreView.AllRemove();
            _pointParticlePresenter.AllRemove();
            // クリア表示・リザルト画面は GameClearUseCase が出す
            _gameStateDataStore.SetCleared();
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
