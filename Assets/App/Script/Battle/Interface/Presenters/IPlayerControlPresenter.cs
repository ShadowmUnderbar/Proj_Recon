using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface.Presenters
{
    public interface IPlayerControlPresenter
    {
        void Move(Vector2 moveV2);
        void Aim();
        void Shot(bool IsLeft);
    }
}