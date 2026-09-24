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

        /// <summary>バウンズ換算でスケールを割るときの下限</summary>
        private const float MinScaleForDivision = 0.0001f;

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

                // 絶対値を取り、現実的な下限で割る。スケール0やミラー配置の負値だと
                // Mathf.Epsilon では商が無限大になり、Invalid AABB でカリングが壊れる
                var scaleY = Mathf.Max(Mathf.Abs(target.transform.lossyScale.y), MinScaleForDivision);
                var drop = _config.CullingDropMargin / scaleY;

                // 下端だけを drop ぶん延ばす。中心も半分だけ下げないと上端まで一緒に伸びてしまう
                bounds.center -= new Vector3(0f, drop * 0.5f, 0f);
                bounds.extents += new Vector3(0f, drop * 0.5f, 0f);
                target.localBounds = bounds;
            }
        }
    }
}
