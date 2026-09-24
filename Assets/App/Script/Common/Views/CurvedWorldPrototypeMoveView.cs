using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace App.Common.Views
{
    /// <summary>
    /// 水平線プロトタイプ用の移動。カーブの中心となるオブジェクトを左スティックで動かす。
    /// 建物が水平線からせり上がってくるかは、止まって見ていてもわからない。
    /// 本編の移動処理とは独立させたいのでここに置いてある。
    /// </summary>
    public class CurvedWorldPrototypeMoveView : MonoBehaviour
    {
        /// <summary>スティックの遊び</summary>
        private const float StickDeadZone = 0.15f;

        private const string JoystickControlName = "joystick";

        [SerializeField, Min(0f), Tooltip("移動速度[m/s]")]
        private float _speed = 12f;

        [SerializeField, Tooltip("進行方向の基準にするカメラ。未指定ならワールド軸をそのまま使う")]
        private Transform _directionSource;

        private void Update()
        {
            var input = ReadMoveInput();
            if (input.sqrMagnitude < StickDeadZone * StickDeadZone)
            {
                return;
            }

            var direction = ToWorldDirection(input);
            transform.position += direction * (_speed * Time.deltaTime);
        }

        /// <summary>スティックの入力をカメラの向きに合わせた水平方向へ直す</summary>
        private Vector3 ToWorldDirection(Vector2 input)
        {
            if (_directionSource == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            var forward = _directionSource.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < Mathf.Epsilon)
            {
                // 真下を向いていて水平成分が消えたときは、頭の上方向を前方とみなす
                forward = _directionSource.up;
                forward.y = 0f;
            }

            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            return right * input.x + forward * input.y;
        }

        private static Vector2 ReadMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var keyInput = new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

                if (keyInput.sqrMagnitude > 0f)
                {
                    return keyInput.normalized;
                }
            }

            var stick = XRController.leftHand?.TryGetChildControl<Vector2Control>(JoystickControlName);
            return stick?.ReadValue() ?? Vector2.zero;
        }
    }
}
