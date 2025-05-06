using R3;
using UnityEngine;

namespace App.Common.Interface
{
    public interface IGameInputUseCase
    {
        ReactiveProperty<bool> IsRightTrigger { get; }
        ReactiveProperty<bool> IsLeftTrigger { get; }
        ReactiveProperty<bool> IsRightGrip { get; }
        ReactiveProperty<bool> IsLeftGrip { get; }
        Vector2 V2RightAxis { get; set; }
        Vector2 V2LeftAxis { get; set; }
        ReactiveProperty<bool> IsAButton { get; }
        ReactiveProperty<bool> IsBButton { get; }
        ReactiveProperty<bool> IsXButton { get; }
        ReactiveProperty<bool> IsYButton { get; }
        ReactiveProperty<bool> IsRightStick { get; }
        ReactiveProperty<bool> IsLeftStick { get; }
        ReactiveProperty<bool> IsDodge { get; }
        public Vector2 MouseInputPosition { get; }
    }
}