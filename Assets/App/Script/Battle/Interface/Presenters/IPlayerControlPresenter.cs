using App.Battle.Data;
using App.Common.Data;
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
        void Shot(ShotType shotType, int focusTargetIdint, bool isLeft);
        void MouseAim(Vector2 mousePos);
    }
}