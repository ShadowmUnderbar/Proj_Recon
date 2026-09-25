using UnityEngine;

namespace App.MainMenu.Interface
{
    public interface IMenuLocomotionPresenter
    {
        void Move(Vector2 input, float speed);

        void SnapTurn(float angleDegrees);

        void SetTeleportAiming(bool aiming);

        bool TeleportToAim();
    }
}
