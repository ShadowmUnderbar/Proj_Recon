using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;
using App.Battle.Interface.Views;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IPlayerMoveView _playerMoveView;
        private readonly IPlayerTopDownAimStoreView _playerTopDownAimStoreView;

        [Inject]
        public PlayerControlPresenter(
            IPlayerMoveView playerMoveView,
            IPlayerTopDownAimStoreView playerTopDownAimStoreView
        )
        {
            _playerMoveView = playerMoveView;
            _playerTopDownAimStoreView = playerTopDownAimStoreView;
        }

        public void Move(Vector2 moveV2)
        {
            _playerMoveView.Move(moveV2);
        }

        public void Shot(bool IsLeft)
        {
            _playerTopDownAimStoreView.Shot(IsLeft);
        }

        public void Aim()
        {
            _playerTopDownAimStoreView.Aim();
        }
    }
}