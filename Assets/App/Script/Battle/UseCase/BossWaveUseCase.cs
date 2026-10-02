using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ボスウェーブの開始時に、前のウェーブから残った敵を消し、プレイヤーを決まった位置へ移してボスグループを出す。
    /// 出現演出は未実装で、ウェーブ開始と同時にその場へ出す。
    /// 湧きの停止は EnemyRandomSpawnCycleDataStore、ウェーブの進行条件は WaveManagerUseCase が IBossWaveDataStore を見て切り替える。
    /// </summary>
    public class BossWaveUseCase : IInitializable, IDisposable
    {
        private readonly IBossWaveDataStore _bossWaveDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly BossWaveConfig _bossWaveConfig;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BossWaveUseCase(
            IBossWaveDataStore bossWaveDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerStateDataStore playerStateDataStore,
            BossWaveConfig bossWaveConfig
        )
        {
            _bossWaveDataStore = bossWaveDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _enemyDataStore = enemyDataStore;
            _playerStateDataStore = playerStateDataStore;
            _bossWaveConfig = bossWaveConfig;
        }

        public void Initialize()
        {
            // ポーズが解けた時点をウェーブ開始として扱う（TutorialWaveUseCase と同じ判定）
            _waveManagerDataStore.IsWavePause
                .Where(isPause => !isPause)
                .Subscribe(_ => OnWaveStarted())
                .AddTo(_disposable);
        }

        private void OnWaveStarted()
        {
            // ボスウェーブ以外・出現済み（同じウェーブ内でポーズが解け直した場合）は何もしない
            if (!_bossWaveDataStore.IsBossWave || _bossWaveDataStore.IsBossSpawned)
            {
                return;
            }

            // 残っていた敵は撃破扱いにせず消す（ポイント・撃破数・撃破時効果は発生しない）
            _enemyDataStore.RemoveAllEnemyData();

            _playerStateDataStore.WarpTo(_bossWaveConfig.PlayerPosition);

            _bossWaveDataStore.TrySpawnBoss();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
