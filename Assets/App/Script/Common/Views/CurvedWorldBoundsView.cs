using System.Collections.Generic;
using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 頂点変位ぶんだけレンダラーのバウンズを下へ広げる。
    /// カリングはCPU側のバウンズで行われ、シェーダが頂点を沈めたことを知らないため、
    /// 広げないと、まだ見えている壁が消えたり、逆に見えないはずの影だけが残ったりする。
    /// </summary>
    public class CurvedWorldBoundsView : MonoBehaviour
    {
        [SerializeField, Tooltip("バウンズを広げる量を読むための設定")]
        private CurvedWorldConfig _config;

        [SerializeField, Tooltip("対象のレンダラー。空なら自分以下のレンダラーをすべて対象にする")]
        private Renderer[] _renderers;

        /// <summary>広げる前のローカルバウンズ。再有効化のたびに積み増さないため元を覚えておく</summary>
        private readonly Dictionary<Renderer, Bounds> _originalBounds = new();

        private void OnEnable()
        {
            Expand();
        }

        /// <summary>対象レンダラーのローカルバウンズを沈下量ぶん下へ広げる</summary>
        public void Expand()
        {
            if (_config == null)
            {
                return;
            }

            var targets = _renderers != null && _renderers.Length > 0
                ? _renderers
                : GetComponentsInChildren<Renderer>(true);

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                // スキンメッシュは毎フレームバウンズを再計算するため、切っておかないと広げた値が消える
                if (target is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = false;
                }

                if (!_originalBounds.TryGetValue(target, out var bounds))
                {
                    bounds = target.localBounds;
                    _originalBounds.Add(target, bounds);
                }

                // 沈むのはワールドの真下だが、localBoundsはローカル軸に沿う。
                // 傾いた壁のようにX/Z方向に回転した対象では、ローカルの-Yは真下ではない。
                // ワールドの下方向をローカルへ変換し、そこへずらした箱ごと包む
                var localDown = target.transform.InverseTransformVector(Vector3.down * _config.CullingDropMargin);
                if (IsFinite(localDown))
                {
                    var expanded = bounds;
                    expanded.Encapsulate(new Bounds(bounds.center + localDown, bounds.size));
                    target.localBounds = expanded;
                }
            }
        }

        /// <summary>スケール0の対象ではInverseTransformVectorが無限大やNaNを返しうる</summary>
        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
