using App.Common.Data;
using App.Common.Interface;
using App.MainMenu.Interface;
using UnityEngine;
using VContainer;

namespace App.MainMenu.Presenters
{
    /// <summary>
    /// メインメニューでの頭と手の姿勢。チュートリアルメッセージ（Common の TutorialMessageUseCase）の追従先として渡す
    /// </summary>
    public class MenuPlayerPosePresenter : IPlayerPosePresenter
    {
        private readonly IMenuPlayerPoseView _menuPlayerPoseView;

        [Inject]
        public MenuPlayerPosePresenter(IMenuPlayerPoseView menuPlayerPoseView)
        {
            _menuPlayerPoseView = menuPlayerPoseView;
        }

        public bool IsHandPoseAvailable => _menuPlayerPoseView.IsHandPoseAvailable;

        public bool TryGetHeadPose(out Pose pose) => _menuPlayerPoseView.TryGetHeadPose(out pose);

        public Pose GetHandPose(HandType hand) => _menuPlayerPoseView.GetHandPose(hand);
    }
}
