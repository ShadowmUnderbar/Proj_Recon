using UnityEngine;
using VContainer.Unity;
using UnityEngine.InputSystem;
using App.Common.Interface;
using R3;
using VContainer;

namespace App.Common.UseCase
{
    public class GameInputUseCase : IGameInputUseCase, IInitializable, ITickable
    {
        public GameMaininput Input { get; } = new();

        private readonly ISaveDataStore _saveDataStore;

        [Inject]
        public GameInputUseCase(
            ISaveDataStore saveDataStore
        )
        {
            _saveDataStore = saveDataStore;
        }

        public void Initialize()
        {
            Input.Enable();
        }

        public ReactiveProperty<bool> IsRightTrigger { get; } = new();
        public ReactiveProperty<bool> IsLeftTrigger { get; } = new();
        public ReactiveProperty<bool> IsFocusRight { get; } = new();
        public ReactiveProperty<bool> IsFocusLeft { get; } = new();
        public Vector2 V2RightAxis { get; set; }
        public Vector2 V2LeftAxis { get; set; }
        public ReactiveProperty<bool> IsAButton { get; } = new();
        public ReactiveProperty<bool> IsBButton { get; } = new();
        public ReactiveProperty<bool> IsXButton { get; } = new();
        public ReactiveProperty<bool> IsYButton { get; } = new();
        public ReactiveProperty<bool> IsRightStick { get; } = new();
        public ReactiveProperty<bool> IsLeftStick { get; } = new();
        public ReactiveProperty<bool> IsDodge { get; } = new();
        public Vector2 MouseInputPosition { get; private set; }

        public void Tick()
        {
            IsRightTrigger.Value = Input.Main.UseRight.inProgress;
            IsLeftTrigger.Value = Input.Main.UseLeft.inProgress;

            if (_saveDataStore.SaveData.IsSwitchableFocus)
            {
                SwitchFocus();
            }
            else
            {
                HoldFocus();
            }

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
            IsDodge.Value = Input.Debug.Dodge.inProgress;
#endif
        }

        private void SwitchFocus()
        {
            if (Input.Main.GrabRight.inProgress)
            {
                IsFocusRight.Value = !IsFocusRight.Value;
            }

            if (Input.Main.GrabLeft.inProgress)
            {
                IsFocusLeft.Value = !IsFocusLeft.Value;
            }
        }

        private void HoldFocus()
        {
            IsFocusRight.Value = Input.Main.FocusRightHold.inProgress;
            IsFocusLeft.Value = Input.Main.FocusLeftHold.inProgress;
        }
    }
}