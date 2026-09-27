using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// バトル中にチュートリアルメッセージを出す手段。
    /// 文言は Localization の TutorialText テーブルから引き、表示中は頭と非利き手の姿勢を毎フレーム View へ渡す。
    /// 新しいメッセージを出すか <see cref="Hide"/> を呼ぶまで表示したままにする。
    /// ラン開始のセット選択に入った時点（シーン開始・リスタート）で前のランのメッセージは消す。
    /// 表示のきっかけ（どのタイミングでどの種類を出すか）と閲覧記録（MarkViewed）は呼び出し側の責務
    /// </summary>
    public class TutorialMessageUseCase : ITutorialMessageUseCase, IInitializable, ITickable, IDisposable
    {
        private readonly ITutorialLocalizationDataStore _tutorialLocalizationDataStore;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly ITutorialMessagePresenter _tutorialMessagePresenter;
        private readonly IRunStartDataStore _runStartDataStore;

        private readonly CompositeDisposable _disposable = new();

        /// <summary>表示中のチュートリアル種類。非表示なら null</summary>
        private TutorialType? _currentType;

        [Inject]
        public TutorialMessageUseCase(
            ITutorialLocalizationDataStore tutorialLocalizationDataStore,
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerControlPresenter playerControlPresenter,
            ITutorialMessagePresenter tutorialMessagePresenter,
            IRunStartDataStore runStartDataStore)
        {
            _tutorialLocalizationDataStore = tutorialLocalizationDataStore;
            _playerSettingDataStore = playerSettingDataStore;
            _playerControlPresenter = playerControlPresenter;
            _tutorialMessagePresenter = tutorialMessagePresenter;
            _runStartDataStore = runStartDataStore;
        }

        public void Initialize()
        {
            // テーブルの読込完了・ロケール切替で表示中の文言を取り直す
            _tutorialLocalizationDataStore.OnTableChanged
                .Where(_ => _currentType.HasValue)
                .Subscribe(_ => _tutorialMessagePresenter.SetText(GetText(_currentType.Value)))
                .AddTo(_disposable);

            // リスタートでセット選択へ戻ったとき、前のランのメッセージを持ち越さない
            _runStartDataStore.IsSelecting
                .Where(isSelecting => isSelecting)
                .Subscribe(_ => Hide())
                .AddTo(_disposable);
        }

        /// <summary>指定種類のメッセージを表示する。表示中でも差し替えて視点正面からやり直す</summary>
        public void Show(TutorialType type)
        {
            _currentType = type;
            _tutorialMessagePresenter.Show(GetText(type));
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

            if (!_playerControlPresenter.TryGetGazePose(out var headPose))
            {
                return;
            }

            var hand = _playerSettingDataStore.NonDominantHand;
            var handPose = hand == HandType.Left
                ? _playerControlPresenter.LeftHandPose.Value
                : _playerControlPresenter.RightHandPose.Value;

            // 手の姿勢が更新されない（非VR）間は、手の追従は使わず視点の正面に留める
            _tutorialMessagePresenter.UpdateAnchor(
                new TutorialMessageAnchor(headPose, hand, handPose, _playerControlPresenter.IsHandPoseAvailable));
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
