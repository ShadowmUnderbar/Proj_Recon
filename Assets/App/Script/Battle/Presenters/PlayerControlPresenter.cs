using UnityEngine;
using App.Battle.Interface;
using VContainer;
using R3;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IBattlePlayerView _playerView;


        public Observable<int> OnFocus => _playerView.OnFocus;
        public Observable<int> OnUnFocus => _playerView.OnUnFocus;

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