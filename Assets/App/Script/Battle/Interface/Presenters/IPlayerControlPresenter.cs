using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface.Presenters
{
    public interface IPlayerControlPresenter
    {
        void Move(Vector2 moveV2);

        void AimLeft(Vector2 position);
        void AimRight(Vector2 position);
    }
}