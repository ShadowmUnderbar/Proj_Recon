using UnityEngine;
using VContainer.Unity;
using App.Common.Interface.UseCase;

namespace App.Common.UseCase
{
    public class GameInputUsecase : IGameInputUsecase, ITickable
    {
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
            
        }
    }
}