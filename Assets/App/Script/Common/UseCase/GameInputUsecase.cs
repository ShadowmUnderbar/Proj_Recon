using UnityEngine;
using VContainer.Unity;
using App.Common.Interface.UseCase;

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

        public bool IsRightTrigger { get; set; }
        public bool IsLeftTrigger { get; set; }
        public bool IsRightGrip { get; set; }
        public bool IsLeftGrip { get; set; }
        public Vector2 V2RightAxis { get; set; }
        public Vector2 V2LeftAxis { get; set; }
        public bool IsAButton { get; set; }
        public bool IsBButton { get; set; }
        public bool IsXButton { get; set; }
        public bool IsYButton { get; set; }
        public bool IsRightStick { get; set; }
        public bool IsLeftStick { get; set; }

        public void Tick()
        {
            IsRightTrigger = Input.Main.UseRight.inProgress;
            IsLeftTrigger = Input.Main.UseLeft.inProgress;
            IsRightGrip = Input.Main.GrabRight.inProgress;
            IsLeftGrip = Input.Main.GrabLeft.inProgress;

            IsAButton = Input.Main.RightPrimary.inProgress;
            IsBButton = Input.Main.RightSecondary.inProgress;
            IsXButton = Input.Main.LeftSecondary.inProgress;
            IsYButton = Input.Main.LeftSecondary.inProgress;

            V2RightAxis = Input.Main.RightStickAxis.ReadValue<Vector2>();
            V2LeftAxis = Input.Main.LeftStickAxis.ReadValue<Vector2>();

            IsRightStick = Input.Main.PushRightStick.inProgress;
            IsLeftStick = Input.Main.PushLeftStick.inProgress;
        }
    }
}