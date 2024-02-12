using UnityEngine;

namespace App.Battle.Interface.Presenters
{
    public interface IPlayerControlPresenter
    {
        void Move(Vector2 moveV2, float speed);
        void Aim();
        void Shot(bool IsLeft);
    }
}