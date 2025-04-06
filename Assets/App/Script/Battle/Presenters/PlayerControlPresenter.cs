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

        [Inject]
        public PlayerControlPresenter(
            IBattlePlayerView playerView
        )
        {
            _playerView = playerView;
        }

        public void Move(Vector2 moveV2, float speed)
        {
            _playerView.Move(moveV2, speed);
        }

        public void MouseAim(Vector2 mousePos)
        {
            _playerView.MouseAim(mousePos);
        }

        public void Shot(ShotType shotType, int focusTargetIdint, bool isLeft)
        {
            _playerView.Shot(shotType, focusTargetIdint, isLeft);
        }

        public void Aim()
        {
            _playerView.Aim();
        }

        public void SetRayColor(Color color)
        {
            _playerView.SetRayColor(color);
        }
    }
}