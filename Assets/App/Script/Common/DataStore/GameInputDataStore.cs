using App.Common.Interface;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace App.Common.DataStore
{
    public class GameInputDataStore : IGameInputDataStore, IInitializable, ITickable
    {
        public GameMaininput Input { get; } = new();

        private readonly ISaveDataStore _saveDataStore;

        [Inject]
        public GameInputDataStore(
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
        public ReactiveProperty<bool> IsGrabRight { get; } = new();
        public ReactiveProperty<bool> IsGrabLeft { get; } = new();
        public Vector2 V2RightAxis { get; set; }
        public Vector2 V2LeftAxis { get; set; }
        public ReactiveProperty<bool> IsAButton { get; } = new();
        public ReactiveProperty<bool> IsBButton { get; } = new();
        public ReactiveProperty<bool> IsXButton { get; } = new();
        public ReactiveProperty<bool> IsYButton { get; } = new();
        public ReactiveProperty<bool> IsRightStick { get; } = new();
        public ReactiveProperty<bool> IsLeftStick { get; } = new();
        public ReactiveProperty<bool> IsDodge { get; } = new();
        public ReactiveProperty<bool> DebugNormal { get; } = new();
        public ReactiveProperty<bool> DebugWaltz { get; } = new();
        public ReactiveProperty<bool> DebugMerge { get; } = new();
        public ReactiveProperty<bool> DebugOpenUpgradeShop { get; } = new();
        public Vector2 MouseInputPosition { get; private set; }
        public Vector2 MouseDelta { get; private set; }
        public bool IsMouseRightButtonPressed { get; private set; }

        private bool _isFocusInputEnabled = true;

        /// <summary>
        /// フォーカス入力を受け付けるかどうか。アップグレードカードの掴みでグラブを長押しする間は、
        /// フォーカスが意図せず切り替わってしまうため止める
        /// </summary>
        public void SetFocusInputEnable(bool enable)
        {
            _isFocusInputEnabled = enable;

            if (enable)
            {
                return;
            }

            // 止めるときは非フォーカスへ揃え、再開後の状態が入力に依らず決まるようにする
            IsFocusRight.Value = false;
            IsFocusLeft.Value = false;
        }

        public void Tick()
        {
            IsRightTrigger.Value = Input.Main.UseRight.inProgress;
            IsLeftTrigger.Value = Input.Main.UseLeft.inProgress;

            // グラブは通常フォーカス切替に使うが、UI（アップグレードカード）の掴み判定でも参照するため素の状態も公開する
            IsGrabRight.Value = Input.Main.GrabRight.inProgress;
            IsGrabLeft.Value = Input.Main.GrabLeft.inProgress;

            if (_isFocusInputEnabled)
            {
                if (_saveDataStore.SaveData.IsSwitchableFocus)
                {
                    SwitchFocus();
                }
                else
                {
                    HoldFocus();
                }
            }

            IsAButton.Value = Input.Main.RightPrimary.inProgress;
            IsBButton.Value = Input.Main.RightSecondary.inProgress;
            IsXButton.Value = Input.Main.LeftSecondary.inProgress;
            IsYButton.Value = Input.Main.LeftSecondary.inProgress;

            V2RightAxis = Input.Main.RightStickAxis.ReadValue<Vector2>();
            V2LeftAxis = Input.Main.LeftStickAxis.ReadValue<Vector2>();

            IsRightStick.Value = Input.Main.PushRightStick.inProgress;
            IsLeftStick.Value = Input.Main.PushLeftStick.inProgress;
            IsDodge.Value = Input.Main.Dodge.inProgress;

            // アップグレードカードのクリック判定で使うためエディタ限定にしない
            // （現状ビルド後は DebugConfig.IsVRMode が常にtrueでこの経路は通らないが、
            // 非VRのPCビルドを出すようになったときにここで詰まらないようにしておく）。
            // VR実機にはマウスが無く Mouse.current が null になるため、その場合は更新しない
            if (Mouse.current != null)
            {
                MouseInputPosition = Mouse.current.position.ReadValue();
                MouseDelta = Mouse.current.delta.ReadValue();
                IsMouseRightButtonPressed = Mouse.current.rightButton.isPressed;
            }
            else
            {
                // マウスを抜いたときに最後の値が残ると、視点が回り続けるなど入力が張り付くため戻す
                MouseDelta = Vector2.zero;
                IsMouseRightButtonPressed = false;
            }

#if UNITY_EDITOR
            DebugNormal.Value = Input.Debug.ShotModeNormal.inProgress;
            DebugWaltz.Value = Input.Debug.ShotModeWaltz.inProgress;
            DebugMerge.Value = Input.Debug.ShotModeMerge.inProgress;
            DebugOpenUpgradeShop.Value = Input.Debug.OpenUpgradeShop.inProgress;
#endif
        }

        private void SwitchFocus()
        {
            if (Input.Main.GrabRight.inProgress)
            {
                IsFocusRight.Value = Input.Main.GrabRight.inProgress;
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