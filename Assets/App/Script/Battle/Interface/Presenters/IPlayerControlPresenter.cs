using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface.Presenters
{
    public interface IPlayerControlPresenter
    {
        void Move(Vector2 moveV2);

        void ShotLeft(Vector3 pos);
        void ShotRight(Vector3 pos);
    }
}