using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerMoveView
    {
        void Move(Vector2 move,float speed);
    }
}