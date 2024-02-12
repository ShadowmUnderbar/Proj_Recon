using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IPlayerMoveView
    {
        void Move(Vector2 move);
    }
}