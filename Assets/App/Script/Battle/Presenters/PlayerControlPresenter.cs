using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

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
        _playerAim= playerAim;
    }

    public void Move(Vector2 moveV2)
    {
        _playerMove.Move(moveV2);
    }

    public void Aim(Vector2 angleV2)
    {
        _playerAim.Aim(angleV2);
    }
}
