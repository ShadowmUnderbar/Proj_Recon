using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// カードを探すときの判定範囲の設定。
    /// <see cref="UpgradeCardHoldSettings"/> と同じく、ボードの Inspector 値を毎フレーム束ねて渡す
    /// </summary>
    public readonly struct UpgradeCardGrabSettings
    {
        /// <summary>手を重ねて掴める距離[m]</summary>
        public readonly float DirectGrabDistance;

        /// <summary>レイで掴める距離[m]</summary>
        public readonly float RayGrabDistance;

        /// <summary>カードのコライダーが属するレイヤー</summary>
        public readonly LayerMask CardLayerMask;

        public UpgradeCardGrabSettings(float directGrabDistance, float rayGrabDistance, LayerMask cardLayerMask)
        {
            DirectGrabDistance = directGrabDistance;
            RayGrabDistance = rayGrabDistance;
            CardLayerMask = cardLayerMask;
        }
    }
}
