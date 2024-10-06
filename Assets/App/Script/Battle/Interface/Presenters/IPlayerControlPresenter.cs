using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerControlPresenter
    {
        void Move(Vector2 moveV2, float speed);
        void Aim();
        void Shot(bool isLeft);
        void MouseAim(Vector2 mousePos);
    }
}