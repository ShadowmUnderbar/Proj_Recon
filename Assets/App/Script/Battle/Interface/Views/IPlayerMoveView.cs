using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerMoveView
    {
        ReactiveProperty<Vector3> OnUpdatePosition { get; }
        void Move(Vector2 move,float speed);
    }
}