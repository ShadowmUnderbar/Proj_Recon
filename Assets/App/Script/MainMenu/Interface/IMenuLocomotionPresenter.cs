using UnityEngine;

namespace App.MainMenu.Interface
{
    public interface IMenuLocomotionPresenter
    {
        void Move(Vector2 input, float speed);

        void SnapTurn(float angleDegrees);

        void Look(float yawDegrees, float pitchDegrees);

        void SetTeleportAiming(bool aiming);

        bool TeleportToAim();
    }
}
