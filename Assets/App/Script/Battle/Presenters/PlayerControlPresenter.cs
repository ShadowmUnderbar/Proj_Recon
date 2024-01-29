using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IPlayerMove _playerMove;
        private readonly IPlayerAim _playerAim;
        private readonly IPlayerShot _playerShot;

        [Inject]
        PlayerControlPresenter(
            IPlayerMove playerMove,
            IPlayerAim playerAim,
            IPlayerShot playerShot
        )
        {
            _playerMove = playerMove;
            _playerAim = playerAim;
            _playerShot = playerShot;
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
    }
}