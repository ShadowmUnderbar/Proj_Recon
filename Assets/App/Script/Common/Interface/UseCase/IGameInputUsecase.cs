using R3;
using UnityEngine;

namespace App.Common.Interface
{
    public interface IGameInputUseCase
    {
        ReactiveProperty<bool> IsRightTrigger { get; }
        ReactiveProperty<bool> IsLeftTrigger { get; }
        Vector2 V2RightAxis { get; set; }
        Vector2 V2LeftAxis { get; set; }
        ReactiveProperty<bool> IsFocusRight { get; }
        ReactiveProperty<bool> IsFocusLeft { get; }
        ReactiveProperty<bool> IsXButton { get; }
        ReactiveProperty<bool> IsYButton { get; }
        ReactiveProperty<bool> IsRightStick { get; }
        ReactiveProperty<bool> IsLeftStick { get; }
        ReactiveProperty<bool> IsDodge { get; }
        public Vector2 MouseInputPosition { get; }
    }
}