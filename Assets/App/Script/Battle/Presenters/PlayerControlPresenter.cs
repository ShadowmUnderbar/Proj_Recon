using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;
using App.Battle.Interface.View;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IPlayerMove _playerMove;
        private readonly IPlayerShot _playerShot;
        private readonly IPlayerTopDownAimStoreView _playerTopDownAimStoreView;

        [Inject]
        PlayerControlPresenter(
            IPlayerMove playerMove,
            IPlayerShot playerShot,
            IPlayerTopDownAimStoreView playerTopDownAimStoreView
        )
        {
            _playerMove = playerMove;
            _playerShot = playerShot;
            _playerTopDownAimStoreView = playerTopDownAimStoreView;
        }

        public void Move(Vector2 moveV2)
        {
            _playerMove.Move(moveV2);
        }

        public void ShotLeft(Vector3 pos)
        {
            _playerShot.ShotLeft(pos);
        }

        public void ShotRight(Vector3 pos)
        {
            _playerShot.ShotRight(pos);
        }

        public void Aim()
        {
            _playerTopDownAimStoreView.Aim();
        }
    }
}