using App.Common.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace App.Common.Views
{
    /// <summary>
    /// 実機で見渡せる距離を振るためのプロトタイプ用コンポーネント。
    /// Quest向けのビルドは1回数分かかるため、値を変えるたびに焼き直していると
    /// 見え方の当たりを取る前に日が暮れる。
    /// 共有のInputActionsには手を入れず、デバイスを直接読んでいる。
    /// 書き換えるのは実行中の値だけで、設定アセットには触れない。
    /// 気に入った値はログに出るので、Inspectorへ手で書き戻す。
    /// </summary>
    public class CurvedWorldTunerView : MonoBehaviour
    {
        /// <summary>スティックの遊び。これ以下の傾きは無視する</summary>
        private const float StickDeadZone = 0.2f;

        /// <summary>キーボード操作時のスティック相当の入力量</summary>
        private const float KeyboardInput = 1f;

        /// <summary>ログを出す最短間隔[秒]。毎フレーム出すと実機のログが埋まる</summary>
        private const float LogInterval = 0.5f;

        private const string JoystickControlName = "joystick";
        private const string GripControlName = "gripPressed";

        [SerializeField, Tooltip("見渡せる距離を書き換える対象")]
        private CurvedWorldView _curvedWorldView;

        [SerializeField, Tooltip("両手のグリップを握っている間だけ調整を受け付ける。通常プレイの操作と衝突させないため")]
        private bool _requireBothGrips = true;

        private float _nextLogTime;

        private void Update()
        {
            if (_curvedWorldView == null)
            {
                return;
            }

            var input = ReadKeyboardInput();
            if (Mathf.Approximately(input, 0f))
            {
                input = ReadControllerInput();
            }

            if (Mathf.Approximately(input, 0f))
            {
                return;
            }

            var config = _curvedWorldView.Config;
            var changePerSecond = config != null ? config.HorizonChangePerSecond : 0f;
            _curvedWorldView.AdjustHorizonDistance(input * changePerSecond * Time.deltaTime);

            LogCurrentHorizon();
        }

        /// <summary>実機ではadb logcatでこの値を読み、良い値をInspectorへ書き戻す</summary>
        private void LogCurrentHorizon()
        {
            if (Time.unscaledTime < _nextLogTime)
            {
                return;
            }

            _nextLogTime = Time.unscaledTime + LogInterval;
            Debug.Log(
                $"[CurvedWorldTunerView] 見える範囲={_curvedWorldView.RuntimeHorizonDistance:F0}m " +
                $"曲率={_curvedWorldView.Displacement.Strength:F5}");
        }

        /// <summary>PCデバッグ用。PageUpで強く、PageDownで弱くする</summary>
        private static float ReadKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return 0f;
            }

            if (keyboard.pageUpKey.isPressed)
            {
                return KeyboardInput;
            }

            return keyboard.pageDownKey.isPressed ? -KeyboardInput : 0f;
        }

        /// <summary>右コントローラのスティック上下。修飾が有効なら両グリップを握っている間だけ効く</summary>
        private float ReadControllerInput()
        {
            var right = XRController.rightHand;
            if (right == null)
            {
                return 0f;
            }

            if (_requireBothGrips && !(IsGripPressed(XRController.leftHand) && IsGripPressed(right)))
            {
                return 0f;
            }

            var stick = right.TryGetChildControl<Vector2Control>(JoystickControlName);
            if (stick == null)
            {
                return 0f;
            }

            var value = stick.ReadValue().y;
            return Mathf.Abs(value) < StickDeadZone ? 0f : value;
        }

        private static bool IsGripPressed(XRController controller)
        {
            var grip = controller?.TryGetChildControl<ButtonControl>(GripControlName);
            return grip != null && grip.isPressed;
        }
    }
}
