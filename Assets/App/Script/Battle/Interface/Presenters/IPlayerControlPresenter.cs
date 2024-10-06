using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerControlPresenter
    {
        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }
        void Move(Vector2 moveV2, float speed);
        void Aim();
        void Shot(bool isLeft);
        void MouseAim(Vector2 mousePos);
    }
}