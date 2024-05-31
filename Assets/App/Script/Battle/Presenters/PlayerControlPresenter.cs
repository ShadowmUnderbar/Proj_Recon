using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;
using App.Battle.Interface.Views;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IBattlePlayerView _playerView;

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

        public void Shot(bool isLeft)
        {
            _playerView.Shot(isLeft);
        }

        public void Aim()
        {
            _playerView.Aim();
        }
    }
}