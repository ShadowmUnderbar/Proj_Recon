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
    /// 購読した時点で既に解けていれば（メインメニュー経由で即開始）その場でウェーブ1として扱う。
    /// 閲覧済み（規定回数）のものは再表示設定が有効でない限り出さず、表示したら閲覧回数を記録する。
    /// 割り当てのないウェーブ（または割り当てはあるが閲覧済みで出さないウェーブ）では、前のメッセージが残っていれば消す
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
            // ポーズが解けている状態をウェーブ開始として扱う。
            // メインメニュー経由やスロットが全部空のときは RunStartUseCase の初期化中に
            // ウェーブ1が始まる（このクラスより先に初期化される）ため、購読時点で既に解けていれば即座に扱う
            _waveManagerDataStore.IsWavePause
                .Where(isPause => !isPause)
                .Subscribe(_ => OnWaveStarted(_waveManagerDataStore.CurrentWave.CurrentValue))
                .AddTo(_disposable);
        }

        private void OnWaveStarted(int wave)
        {
            if (_tutorialWaveConfig.TryGetTutorial(wave, out var type) && _tutorialProgressDataStore.ShouldShow(type))
            {
                _tutorialMessageUseCase.Show(type);
                _tutorialProgressDataStore.MarkViewed(type);
                return;
            }

            // このウェーブで出すものが無ければ、前のウェーブのメッセージを持ち越さない
            _tutorialMessageUseCase.Hide();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
