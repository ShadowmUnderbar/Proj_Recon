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

        [Inject]
        PlayerControlPresenter(
            IPlayerMove playerMove,
            IPlayerAim playerAim
        )
        {
            _playerMove = playerMove;
            _playerAim = playerAim;
        }

        public void Move(Vector2 moveV2)
        {
            _playerMove.Move(moveV2);
        }

        public void AimLeft(Vector2 angleV2)
        {
        }

        public void AimRight(Vector2 angleV2)
        {
        }
    }
}