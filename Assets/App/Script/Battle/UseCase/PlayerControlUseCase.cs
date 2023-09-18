using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class PlayerControlUseCase : IPlayerControlUseCase
{
    private IPlayerControlPresenter _playerControlPresenter;

    [Inject]
    public PlayerControlUseCase(
        IPlayerControlPresenter playerControlPresenter
    )
    {
        _playerControlPresenter = playerControlPresenter;
    }

    public void Move(Vector2 moveV2)
    {
        _playerControlPresenter.Move(moveV2);
    }

    public void Aim(Vector2 angleV2)
    {
        _playerControlPresenter.Move(angleV2);
    }
}
