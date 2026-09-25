using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 非VR（PC/エディタ）でアップグレードカードを選ぶための、マウスポインタの入力スナップショット。
    /// UseCase が毎フレーム作って View へ渡し、View 側で自分のカメラを使ってレイに変換する
    /// </summary>
    public readonly struct ShopPointerInput
    {
        /// <summary>ポインタのスクリーン座標[px]</summary>
        public readonly Vector2 ScreenPosition;

        /// <summary>決定ボタン（マウス左ボタン）を押しているか</summary>
        public readonly bool IsPressed;

        public ShopPointerInput(Vector2 screenPosition, bool isPressed)
        {
            ScreenPosition = screenPosition;
            IsPressed = isPressed;
        }
    }
}
