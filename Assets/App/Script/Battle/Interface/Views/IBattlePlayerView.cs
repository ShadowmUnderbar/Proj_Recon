using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IBattlePlayerView
    {
        void Move(Vector2 _inputV2, float speed);
        void Aim();
        void MouseAim(Vector2 mousePos);
        void SetFocus(bool isLeft);
        void Shot(bool isLeft);
    }
}