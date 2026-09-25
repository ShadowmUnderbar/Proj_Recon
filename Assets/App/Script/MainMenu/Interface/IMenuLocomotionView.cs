using UnityEngine;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// メインメニューの部屋を一人称で歩き回るための移動。
    /// 入力の読み取りと移動方式の切り替えはUseCaseが行い、Viewは指示された移動だけを実行する。
    /// </summary>
    public interface IMenuLocomotionView
    {
        /// <summary>スティック入力ぶんだけ歩く。方向はHMDの向き基準に解釈される</summary>
        /// <param name="input">スティック入力（デッドゾーン処理済み）</param>
        /// <param name="speed">移動速度[m/s]</param>
        void Move(Vector2 input, float speed);

        /// <summary>その場で指定角度だけ向きを変える。回転の中心はHMDの位置</summary>
        void SnapTurn(float angleDegrees);

        /// <summary>テレポート先の照準（レイとマーカー）の表示を切り替える</summary>
        void SetTeleportAiming(bool aiming);

        /// <summary>現在の照準先へ移動する。着地できる床を指していなければ何もしない</summary>
        /// <returns>移動できたか</returns>
        bool TeleportToAim();
    }
}
