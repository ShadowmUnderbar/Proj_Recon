using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ウェーブ開始時に <see cref="TutorialWaveConfig"/> で割り当てたチュートリアルを表示する。
    /// ウェーブの開始は「ポーズ解除」で判定する（ウェーブ1はセット選択の解除、以降はショップの「次のウェーブへ」）。
    /// 閲覧済み（規定回数）のものは再表示設定が有効でない限り出さず、表示したら閲覧回数を記録する。
    /// 割り当てのないウェーブでは前のメッセージをそのまま残す（消すのは新しいメッセージか明示の Hide のみ）
    /// </summary>
    public class TutorialWaveUseCase : IInitializable, IDisposable
    {
        private readonly TutorialWaveConfig _tutorialWaveConfig;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly ITutorialProgressDataStore _tutorialProgressDataStore;
        private readonly ITutorialMessageUseCase _tutorialMessageUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public TutorialWaveUseCase(
            TutorialWaveConfig tutorialWaveConfig,
            IWaveManagerDataStore waveManagerDataStore,
            ITutorialProgressDataStore tutorialProgressDataStore,
            ITutorialMessageUseCase tutorialMessageUseCase)
        {
            _tutorialWaveConfig = tutorialWaveConfig;
            _waveManagerDataStore = waveManagerDataStore;
            _tutorialProgressDataStore = tutorialProgressDataStore;
            _tutorialMessageUseCase = tutorialMessageUseCase;
        }

        public void Initialize()
        {
            // 初期値（ポーズ中）は無視し、ポーズが解けた瞬間だけをウェーブ開始として扱う
            _waveManagerDataStore.IsWavePause
                .Skip(1)
                .Where(isPause => !isPause)
                .Subscribe(_ => OnWaveStarted(_waveManagerDataStore.CurrentWave.CurrentValue))
                .AddTo(_disposable);
        }

        private void OnWaveStarted(int wave)
        {
            if (!_tutorialWaveConfig.TryGetTutorial(wave, out var type))
            {
                return;
            }

            if (!_tutorialProgressDataStore.ShouldShow(type))
            {
                return;
            }

            _tutorialMessageUseCase.Show(type);
            _tutorialProgressDataStore.MarkViewed(type);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
