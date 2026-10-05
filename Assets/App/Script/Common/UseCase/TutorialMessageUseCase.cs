using System;
using App.Common.Data;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Common.UseCase
{
    /// <summary>
    /// チュートリアルメッセージを出す手段（バトル・メインメニュー共通）。
    /// 文言は Localization の TutorialText テーブルから引き、表示中は頭と非利き手の姿勢を毎フレーム View へ渡す。
    /// 新しいメッセージを出すか <see cref="Hide"/> を呼ぶまで表示したままにする。
    /// 表示のきっかけ（どのタイミングでどの種類を出すか・いつ消すか）は呼び出し側の責務。
    /// 閲覧記録は <see cref="ShowIfNeeded"/> を使えばここで付ける
    /// </summary>
    public class TutorialMessageUseCase : ITutorialMessageUseCase, IInitializable, ITickable, IDisposable
    {
        private readonly ITutorialLocalizationDataStore _tutorialLocalizationDataStore;
        private readonly ITutorialProgressDataStore _tutorialProgressDataStore;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;
        private readonly IPlayerPosePresenter _playerPosePresenter;
        private readonly ITutorialMessagePresenter _tutorialMessagePresenter;

        private readonly CompositeDisposable _disposable = new();

        /// <summary>表示中のチュートリアル種類。非表示なら null</summary>
        private TutorialType? _currentType;

        [Inject]
        public TutorialMessageUseCase(
            ITutorialLocalizationDataStore tutorialLocalizationDataStore,
            ITutorialProgressDataStore tutorialProgressDataStore,
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerPosePresenter playerPosePresenter,
            ITutorialMessagePresenter tutorialMessagePresenter)
        {
            _tutorialLocalizationDataStore = tutorialLocalizationDataStore;
            _tutorialProgressDataStore = tutorialProgressDataStore;
            _playerSettingDataStore = playerSettingDataStore;
            _playerPosePresenter = playerPosePresenter;
            _tutorialMessagePresenter = tutorialMessagePresenter;
        }

        public void Initialize()
        {
            // テーブルの読込完了・ロケール切替で表示中の文言を取り直す
            _tutorialLocalizationDataStore.OnTableChanged
                .Where(_ => _currentType.HasValue)
                .Subscribe(_ => _tutorialMessagePresenter.SetText(GetText(_currentType.Value)))
                .AddTo(_disposable);
        }

        /// <summary>指定種類のメッセージを表示する。表示中でも差し替えて視点正面からやり直す</summary>
        public void Show(TutorialType type)
        {
            _currentType = type;
            _tutorialMessagePresenter.Show(GetText(type));
        }

        public bool ShowIfNeeded(TutorialType type)
        {
            if (!_tutorialProgressDataStore.ShouldShow(type))
            {
                return false;
            }

            Show(type);
            _tutorialProgressDataStore.MarkViewed(type);
            return true;
        }

        public void Hide()
        {
            _currentType = null;
            _tutorialMessagePresenter.Hide();
        }

        public void Tick()
        {
            if (!_currentType.HasValue)
            {
                return;
            }

            if (!_playerPosePresenter.TryGetHeadPose(out var headPose))
            {
                return;
            }

            var hand = _playerSettingDataStore.NonDominantHand;

            // 手の姿勢が更新されない（非VR）間は、手の追従は使わず視点の正面に留める
            _tutorialMessagePresenter.UpdateAnchor(new TutorialMessageAnchor(
                headPose, hand, _playerPosePresenter.GetHandPose(hand), _playerPosePresenter.IsHandPoseAvailable));
        }

        private string GetText(TutorialType type)
        {
            return _tutorialLocalizationDataStore.GetText(type);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
