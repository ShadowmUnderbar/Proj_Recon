using System;
using System.Threading;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// プレイヤーHPが0になったらゲームオーバーにし、ゲームオーバー画面を表示する。
    /// 画面のスロット保存ボタンで、そのランで獲得したアップグレードをメタ進行スロットへ保存する。
    /// 保存は何度でも行え、リスタートボタンでラン状態を初期化してビルド選択へ戻る。
    /// メインメニューボタンではシーンごと切り替えてタイトルへ戻る。
    /// 画面を出す前に死亡演出（ヒットストップ→死亡アニメ→余韻）を挟み、その完了を待つ。
    /// </summary>
    public class GameOverUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly IMetaProgressionDataStore _metaProgressionDataStore;
        private readonly IGameOverPresenter _gameOverPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly RunResetUseCase _runResetUseCase;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;
        private readonly PlayerDeathConfig _playerDeathConfig;

        private readonly CompositeDisposable _disposable = new();

        // 死亡演出の中断用。リスタートやシーン終了で演出を打ち切る
        private CancellationTokenSource _deathSequenceCts;

        [Inject]
        public GameOverUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            IGameOverPresenter gameOverPresenter,
            IPlayerControlPresenter playerControlPresenter,
            RunResetUseCase runResetUseCase,
            IFreezeDataStore freezeDataStore,
            ISceneTransitionUseCase sceneTransitionUseCase,
            PlayerDeathConfig playerDeathConfig
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _gameOverPresenter = gameOverPresenter;
            _playerControlPresenter = playerControlPresenter;
            _runResetUseCase = runResetUseCase;
            _freezeDataStore = freezeDataStore;
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _playerDeathConfig = playerDeathConfig;
        }

        public void Initialize()
        {
            // HPが0以下になったらゲームオーバー
            _playerStateDataStore.Health
                .Where(health => health <= 0f)
                .Subscribe(_ => OnPlayerDead())
                .AddTo(_disposable);

            // スロット保存ボタン（画面は開いたままなので、続けて別スロットへも保存できる）
            _gameOverPresenter.OnSaveSlotSelected
                .Subscribe(OnSaveSlotSelected)
                .AddTo(_disposable);

            // リスタートボタン
            _gameOverPresenter.OnRestart
                .Subscribe(_ => OnRestart())
                .AddTo(_disposable);

            // メインメニューへ戻るボタン
            _gameOverPresenter.OnReturnToMainMenu
                .Subscribe(_ => OnReturnToMainMenu())
                .AddTo(_disposable);
        }

        private void OnPlayerDead()
        {
            if (_gameStateDataStore.IsGameOver.CurrentValue)
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

                ShowGameOverUi();
            }
            catch (OperationCanceledException)
            {
                // リスタート・シーン終了による中断。画面は出さない
            }
        }

        private void ShowGameOverUi()
        {
            // 保存対象はそのランで新たに獲得した分のみ（セット読込で最初から持っていた分は除外）
            var acquiredCount = _upgradeSessionDataStore.NewlyAcquiredUpgrades.Count;
            _gameOverPresenter.Show($"GAME OVER\n獲得アップグレード: {acquiredCount}個\nスロットに上書き保存してからリスタート");

            RefreshAllSlotLabels();

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);

            if (acquiredCount == 0)
            {
                _gameOverPresenter.SetStatus("このランで新たに獲得したアップグレードはありません");
            }
        }

        // スロットへ上書き保存する。画面は閉じないので、保存先を選び直してからリスタートできる
        private void OnSaveSlotSelected(int slotIndex)
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            var ids = _upgradeSessionDataStore.NewlyAcquiredUpgrades;
            var clearedWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            _metaProgressionDataStore.SaveToSlot(slotIndex, ids, clearedWave);

            // 保存結果をラベル（現在: 〜）と状態テキストの両方に反映する
            RefreshAllSlotLabels();
            _gameOverPresenter.SetStatus($"スロット{slotIndex + 1}に保存しました（{ids.Count}個 / Wave{clearedWave}）");
        }

        /// <summary>
        /// 死亡演出を打ち切り、倒れた姿勢から通常のアニメ・エイムIKへ戻す。
        /// IRunResettable にはしていない（RunResetUseCaseを注入しているため相互依存になる）ので、
        /// リスタート処理から直接呼ぶ。
        /// </summary>
        private void StopDeathSequence()
        {
            _deathSequenceCts?.Cancel();
            _deathSequenceCts?.Dispose();
            _deathSequenceCts = null;

            _playerControlPresenter.ResetDeathAnimation();
        }

        // ラン状態を初期化してビルド選択からやり直す
        private void OnRestart()
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            _gameOverPresenter.Hide();
            _playerControlPresenter.SetUiRayEnable(false);

            // 倒れた姿勢のままランが始まらないよう、リセットの前に演出を畳む
            StopDeathSequence();

            // リセット後のビルド選択UIの表示・ハンドレイの再有効化はRunStartUseCaseが行う
            _runResetUseCase.ResetRun();
        }

        // メインメニューシーンへ戻る。ラン状態はシーンごと破棄されるためリセットは行わない
        private void OnReturnToMainMenu()
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue || _sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            // 遷移を開始できてから画面を畳む。先に畳むと、遷移に失敗したときに
            // 保存もリスタートもできない状態で取り残される
            if (!_sceneTransitionUseCase.LoadMainMenu())
            {
                _gameOverPresenter.SetStatus("メインメニューへ移動できませんでした");
                return;
            }

            _gameOverPresenter.Hide();
            _playerControlPresenter.SetUiRayEnable(false);
            StopDeathSequence();
        }

        private void RefreshAllSlotLabels()
        {
            for (var i = 0; i < _metaProgressionDataStore.SlotCount; i++)
            {
                _gameOverPresenter.SetSlotLabel(i, BuildSlotLabel(i));
            }
        }

        private string BuildSlotLabel(int slotIndex)
        {
            if (_metaProgressionDataStore.IsSlotEmpty(slotIndex))
            {
                return $"スロット{slotIndex + 1}に保存\n(現在: 空)";
            }

            var count = _metaProgressionDataStore.GetSlotUpgradeIds(slotIndex).Count;
            var wave = _metaProgressionDataStore.GetSlotClearedWave(slotIndex);
            return $"スロット{slotIndex + 1}に保存\n(現在: {count}個 / Wave{wave})";
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
