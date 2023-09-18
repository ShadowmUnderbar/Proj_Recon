using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerControlUseCase
{
    void Move(Vector2 vector2);

    void Rotate(Vector2 vector2);
}
