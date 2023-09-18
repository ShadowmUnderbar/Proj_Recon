using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerControlPresenter
{
    void Move(Vector2 vector2);

    void Aim(Vector2 vector2);

}
