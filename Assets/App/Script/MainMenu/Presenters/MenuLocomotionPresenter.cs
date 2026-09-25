using App.MainMenu.Interface;
using UnityEngine;
using VContainer;

namespace App.MainMenu.Presenters
{
    public class MenuLocomotionPresenter : IMenuLocomotionPresenter
    {
        private readonly IMenuLocomotionView _menuLocomotionView;

        [Inject]
        public MenuLocomotionPresenter(IMenuLocomotionView menuLocomotionView)
        {
            _menuLocomotionView = menuLocomotionView;
        }

        public void Move(Vector2 input, float speed) => _menuLocomotionView.Move(input, speed);

        public void SnapTurn(float angleDegrees) => _menuLocomotionView.SnapTurn(angleDegrees);

        public void SetTeleportAiming(bool aiming) => _menuLocomotionView.SetTeleportAiming(aiming);

        public bool TeleportToAim() => _menuLocomotionView.TeleportToAim();
    }
}
