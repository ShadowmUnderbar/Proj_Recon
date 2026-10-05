using System;
using System.Threading;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// プレイヤーHPが0になったらゲームオーバーにし、死亡演出（ヒットストップ→死亡アニメ→余韻）の完了を待って
    /// 結果画面（RunResultUseCase）を出す。保存・リスタート・メインメニューへの操作は RunResultUseCase が担う。
    /// クリアの後にHPが0になってもゲームオーバーにはしない。
    /// </summary>
    public class GameOverUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly RunResultUseCase _runResultUseCase;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly PlayerDeathConfig _playerDeathConfig;

        private readonly CompositeDisposable _disposable = new();

        // 死亡演出の中断用。リスタートやシーン終了で演出を打ち切る
        private CancellationTokenSource _deathSequenceCts;

        [Inject]
        public GameOverUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IPlayerControlPresenter playerControlPresenter,
            RunResultUseCase runResultUseCase,
            IFreezeDataStore freezeDataStore,
            PlayerDeathConfig playerDeathConfig
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _playerControlPresenter = playerControlPresenter;
            _runResultUseCase = runResultUseCase;
            _freezeDataStore = freezeDataStore;
            _playerDeathConfig = playerDeathConfig;
        }

        public void Initialize()
        {
            // HPが0以下になったらゲームオーバー
            _playerStateDataStore.Health
                .Where(health => health <= 0f)
                .Subscribe(_ => OnPlayerDead())
                .AddTo(_disposable);

            // 結果画面からリスタート・メインメニューへ移るときは演出を畳む
            _runResultUseCase.OnClosing
                .Subscribe(_ => StopDeathSequence())
                .AddTo(_disposable);
        }

        private void OnPlayerDead()
        {
            // ゲームオーバー済み・クリア済み（クリア表示中に倒れた場合など）は何もしない
            if (_gameStateDataStore.IsRunEnded)
            {
                return;
            }

            _gameStateDataStore.SetGameOver();

            // ウェーブ進行・スポーンを停止（既存のポーズ機構を流用）
            _waveManagerDataStore.SetWavePause(true);

            // 演出を挟んでからゲームオーバー画面を出す
            _deathSequenceCts?.Cancel();
            _deathSequenceCts?.Dispose();
            _deathSequenceCts = new CancellationTokenSource();

            PlayDeathSequenceAsync(_deathSequenceCts.Token).Forget();
        }

        /// <summary>
        /// 死亡演出。ヒットストップで一拍置いてから死亡アニメを再生し、
        /// その終了と余韻を待ってゲームオーバー画面を出す。
        /// アニメの終了待ちはAnimatorの再生位置で判定するため、クリップを差し替えても長さに追従する。
        /// </summary>
        private async UniTaskVoid PlayDeathSequenceAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 1. ヒットストップ（敵・弾を止める。既存のフリーズ機構を流用）
                if (_playerDeathConfig.HitStopDuration > 0f)
                {
                    _freezeDataStore.Freeze(_playerDeathConfig.HitStopDuration);

                    await UniTask.WaitWhile(
                        () => _freezeDataStore.IsFreezing.CurrentValue,
                        cancellationToken: cancellationToken);
                }

                // 2. 死亡アニメの再生と終了待ち。
                // クリップ未設定などで終わらない場合に画面が出なくなるのを防ぐためタイムアウトを設ける
                _playerControlPresenter.PlayDeathAnimation();

                // タイムアウト時に内側の監視も止めるため、CTSを渡して打ち切れるようにする。
                // 渡さないと画面を出した後もAnimatorへの問い合わせが毎フレーム走り続ける
                using (var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    var isTimeout = await UniTask
                        .WaitUntil(() => _playerControlPresenter.IsDeathAnimationFinished,
                            cancellationToken: waitCts.Token)
                        .TimeoutWithoutException(
                            TimeSpan.FromSeconds(_playerDeathConfig.AnimationTimeout),
                            taskCancellationTokenSource: waitCts);

                    // 外側の中断（リスタート等）はタイムアウトと区別できないため、ここで明示的に打ち切る
                    cancellationToken.ThrowIfCancellationRequested();

                    if (isTimeout)
                    {
                        Debug.LogWarning("[GameOverUseCase] 死亡アニメの終了を待てなかったため、そのままゲームオーバー画面を出します");
                    }
                }

                // 3. 余韻。倒れた瞬間にUIがかぶらないよう少し置く
                if (_playerDeathConfig.PostAnimationDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_playerDeathConfig.PostAnimationDelay),
                        cancellationToken: cancellationToken);
                }

                _runResultUseCase.Show("GAME OVER");
            }
            catch (OperationCanceledException)
            {
                // リスタート・シーン終了による中断。画面は出さない
            }
        }

        /// <summary>
        /// 死亡演出を打ち切り、倒れた姿勢から通常のアニメ・エイムIKへ戻す。
        /// IRunResettable にはしていない（RunResultUseCase 経由で RunResetUseCase に依存しているため相互依存になる）ので、
        /// 結果画面を閉じる通知から呼ぶ。
        /// </summary>
        private void StopDeathSequence()
        {
            _deathSequenceCts?.Cancel();
            _deathSequenceCts?.Dispose();
            _deathSequenceCts = null;

            _playerControlPresenter.ResetDeathAnimation();
        }

        public void Dispose()
        {
            _deathSequenceCts?.Cancel();
            _deathSequenceCts?.Dispose();
            _deathSequenceCts = null;

            _disposable.Dispose();
        }
    }
}
