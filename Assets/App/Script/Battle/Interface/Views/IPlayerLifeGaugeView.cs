using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>
    /// プレイヤーの足元に出す半円のライフゲージ。
    /// 内側の弧でHP、外側の細い弧でバリア残量を表示する。
    /// </summary>
    public interface IPlayerLifeGaugeView
    {
        /// <summary>ゲージを置く足元のワールド座標を設定する</summary>
        void SetPosition(Vector3 position);

        /// <summary>HPの割合（0〜1）を設定する</summary>
        void SetHealthRatio(float ratio);

        /// <summary>バリア残量の割合（0〜1）を設定する</summary>
        void SetBarrierRatio(float ratio);

        /// <summary>バリアの弧を表示するか（バリア未取得なら非表示）</summary>
        void SetBarrierVisible(bool visible);
    }
}
