using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBattlePlayerView
    {
        Observable<HitData> OnHit { get; }

        Observable<int> OnFocus { get; }
        Observable<int> OnUnFocus { get; }
        void Move(Vector2 _inputV2, float speed);
        void Aim();
        void MouseAim(Vector2 mousePos);
        void Shot(bool isLeft);
    }
}