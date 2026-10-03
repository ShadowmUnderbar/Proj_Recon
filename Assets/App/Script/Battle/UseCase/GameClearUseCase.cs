using System;
using System.Threading;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// クリア（ボスウェーブのボスを全員倒した。判定は WaveManagerUseCase）したら、
    /// 見出しだけのクリア表示を出し、規定秒数のあとで結果画面（スロット保存・リスタート・メインメニュー）を出す。
    /// 保存・リスタート・メインメニューへの操作は RunResultUseCase が担う。
    /// </summary>
    public class GameClearUseCase : IInitializable, IDisposable
    {
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly RunResultUseCase _runResultUseCase;
        private readonly GameClearConfig _gameClearConfig;

        private readonly CompositeDisposable _disposable = new();

        // クリア表示の待ちの中断用。リスタートやシーン終了で打ち切る
        private CancellationTokenSource _clearSequenceCts;

        [Inject]
        public GameClearUseCase(
            IGameStateDataStore gameStateDataStore,
            RunResultUseCase runResultUseCase,
            GameClearConfig gameClearConfig
        )
        {
            _gameStateDataStore = gameStateDataStore;
            _runResultUseCase = runResultUseCase;
            _gameClearConfig = gameClearConfig;
        }

        public void Initialize()
        {
            _gameStateDataStore.IsCleared
                .Where(isCleared => isCleared)
                .Subscribe(_ => OnCleared())
                .AddTo(_disposable);

            _runResultUseCase.OnClosing
                .Subscribe(_ => StopClearSequence())
                .AddTo(_disposable);
        }

        private void OnCleared()
        {
            StopClearSequence();
            _clearSequenceCts = new CancellationTokenSource();

            PlayClearSequenceAsync(_clearSequenceCts.Token).Forget();
        }

        private async UniTaskVoid PlayClearSequenceAsync(CancellationToken cancellationToken)
        {
            try
            {
                _runResultUseCase.ShowHeadlineOnly(_gameClearConfig.Headline);

                if (_gameClearConfig.HeadlineOnlyDuration > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_gameClearConfig.HeadlineOnlyDuration),
                        cancellationToken: cancellationToken);
                }

                _runResultUseCase.Show(_gameClearConfig.Headline);
            }
            catch (OperationCanceledException)
            {
                // リスタート・シーン終了による中断。画面は出さない
            }
        }

        private void StopClearSequence()
        {
            _clearSequenceCts?.Cancel();
            _clearSequenceCts?.Dispose();
            _clearSequenceCts = null;
        }

        public void Dispose()
        {
            StopClearSequence();
            _disposable.Dispose();
        }
    }
}
