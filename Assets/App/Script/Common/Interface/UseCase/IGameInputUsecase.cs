using UnityEngine;

namespace App.Common.Interface
{
    public interface IGameInputUsecase
    {
        bool IsRightTrigger { get; set; }
        bool IsLeftTrigger { get; set; }
        bool IsRightGrip { get; set; }
        bool IsLeftGrip { get; set; }
        Vector2 V2RightAxis { get; set; }
        Vector2 V2LeftAxis { get; set; }
        bool IsAButton { get; set; }
        bool IsBButton { get; set; }
        bool IsXButton { get; set; }
        bool IsYButton { get; set; }
        bool IsRightStick { get; set; }
        bool IsLeftStick { get; set; }
        public Vector2 MouseInputPosition { get; }
    }
}