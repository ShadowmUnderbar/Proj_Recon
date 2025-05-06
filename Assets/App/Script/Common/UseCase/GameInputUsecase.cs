using UnityEngine;
using VContainer.Unity;
using UnityEngine.InputSystem;
using App.Common.Interface;
using R3;

namespace App.Common.UseCase
{
    public class GameInputUsecase : IGameInputUsecase, IInitializable, ITickable
    {
        public GameMaininput Input { get; }

        public GameInputUsecase()
        {
            Input = new();
        }

        public void Initialize()
        {
            Input.Enable();
        }

        public ReactiveProperty<bool> IsRightTrigger { get; } = new();
        public ReactiveProperty<bool> IsLeftTrigger { get; } = new();
        public ReactiveProperty<bool> IsRightGrip { get; } = new();
        public ReactiveProperty<bool> IsLeftGrip { get; } = new();
        public Vector2 V2RightAxis { get; set; }
        public Vector2 V2LeftAxis { get; set; }
        public ReactiveProperty<bool> IsAButton { get; } = new();
        public ReactiveProperty<bool> IsBButton { get; } = new();
        public ReactiveProperty<bool> IsXButton { get; } = new();
        public ReactiveProperty<bool> IsYButton { get; } = new();
        public ReactiveProperty<bool> IsRightStick { get; } = new();
        public ReactiveProperty<bool> IsLeftStick { get; } = new();
        public Vector2 MouseInputPosition { get; private set; }

        public void Tick()
        {
            IsRightTrigger.Value = Input.Main.UseRight.inProgress;
            IsLeftTrigger.Value = Input.Main.UseLeft.inProgress;
            IsRightGrip.Value = Input.Main.GrabRight.inProgress;
            IsLeftGrip.Value = Input.Main.GrabLeft.inProgress;

            IsAButton.Value = Input.Main.RightPrimary.inProgress;
            IsBButton.Value = Input.Main.RightSecondary.inProgress;
            IsXButton.Value = Input.Main.LeftSecondary.inProgress;
            IsYButton.Value = Input.Main.LeftSecondary.inProgress;

            V2RightAxis = Input.Main.RightStickAxis.ReadValue<Vector2>();
            V2LeftAxis = Input.Main.LeftStickAxis.ReadValue<Vector2>();

            IsRightStick.Value = Input.Main.PushRightStick.inProgress;
            IsLeftStick.Value = Input.Main.PushLeftStick.inProgress;

#if UNITY_EDITOR
            MouseInputPosition = Mouse.current.position.ReadValue();
#endif
        }
    }
}