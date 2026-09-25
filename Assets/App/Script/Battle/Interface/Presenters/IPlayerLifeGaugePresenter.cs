using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerLifeGaugePresenter
    {
        /// <summary>ゲージを置く足元のワールド座標を設定する</summary>
        void SetPosition(Vector3 position);

        /// <summary>HPの割合（0〜1）を設定する</summary>
        void SetHealthRatio(float ratio);

        /// <summary>バリア残量の割合（0〜1）を設定する</summary>
        void SetBarrierRatio(float ratio);

        /// <summary>バリアの弧を表示するか</summary>
        void SetBarrierVisible(bool visible);
    }
}
