using UnityEngine;
using App.Battle.Interface;
using VContainer;
using R3;
using App.Common.Data;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IBattlePlayerView _playerView;

        public Observable<int> OnFocusLeft => _playerView.OnFocusLeft;
        public Observable<int> OnFocusRight => _playerView.OnFocusRight;
        public Observable<Vector3> OnLeftAimPosition => _playerView.OnLeftAimPosition;
        public Observable<Vector3> OnRightAimPosition => _playerView.OnRightAimPosition;

        public ReactiveProperty<Vector3> OnUpdatePosition => _playerView.OnUpdatePosition;

        public ReactiveProperty<Pose> LeftHandPose => _playerView.LeftHandPose;
        public ReactiveProperty<Pose> RightHandPose => _playerView.RightHandPose;
        public Transform PlayerTransform => _playerView.PlayerTransform;

        [Inject]
        public PlayerControlPresenter(
            IBattlePlayerView playerView
        )
        {
            _playerView = playerView;
        }

        public void Move(Vector2 moveV2)
        {
            _playerView.Move(moveV2);
        }

        public void MouseAim(Vector2 mousePos)
        {
            _playerView.MouseAim(mousePos);
        }

        public void Shot(HandType handType, BulletData bulletData, int focusTargetId)
        {
            _playerView.Shot(handType, bulletData, focusTargetId);
        }

        public void Aim()
        {
            _playerView.Aim();
        }

        public void SetAimRayColor(HandType handType, Color color)
        {
            _playerView.SetAimRayColor(handType, color);
        }

        public void SetHandRayColor(HandType handType, Color color)
        {
            _playerView.SetHandRayColor(handType, color);
        }

        public void SetAimEnableRay(HandType handType, bool enable)
        {
            _playerView.SetAimEnableRay(handType, enable);
        }

        public void SetHandEnableRay(HandType handType, bool enable)
        {
            _playerView.SetHandEnableRay(handType, enable);
        }

        public void IsFocusRight(bool isFocus)
        {
            _playerView.IsFocusRight(isFocus);
        }

        public void IsFocusLeft(bool isFocus)
        {
            _playerView.IsFocusLeft(isFocus);
        }

        public void Blitz(Vector3 startPos, Transform playerTransform)
        {
            _playerView.Blitz(startPos, playerTransform);
        }
    }
}