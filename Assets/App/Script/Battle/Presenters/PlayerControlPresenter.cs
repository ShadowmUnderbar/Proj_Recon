using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;
using App.Battle.Interface.Views;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IPlayerMove _playerMove;
        private readonly IPlayerTopDownAimStoreView _playerTopDownAimStoreView;

        [Inject]
        PlayerControlPresenter(
            IPlayerMove playerMove,
            IPlayerTopDownAimStoreView playerTopDownAimStoreView
        )
        {
            _playerMove = playerMove;
            _playerTopDownAimStoreView = playerTopDownAimStoreView;
        }

        public void Move(Vector2 moveV2)
        {
            _playerMove.Move(moveV2);
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