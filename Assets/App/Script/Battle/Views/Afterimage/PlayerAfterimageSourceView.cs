using System.Collections.Generic;
using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views.Afterimage
{
    /// <summary>
    /// プレイヤーのどの Renderer を残像にするかを持つ。Player.prefab に付ける。
    /// 頭は差し替わる前提で、Renderer を覚えずに「頭の取り付け位置の下にいま表示されているもの」を呼ばれるたびに拾う。
    /// 差し替えの仕組みそのものには触れない。
    /// </summary>
    public class PlayerAfterimageSourceView : MonoBehaviour, IAfterimageSource
    {
        [SerializeField, Tooltip("体の Renderer。残像全体の基準点（欠損模様・水平線カーブの基準）にもなる")]
        private Renderer _bodyRenderer;

        [SerializeField, Tooltip("頭の取り付け位置。この下で表示中の SkinnedMeshRenderer / MeshRenderer を頭として拾う。頭が体と一体のモデルなら空でよい")]
        private Transform _headAnchor;

        private readonly List<Renderer> _headRendererBuffer = new();

        public void CollectRenderers(List<Renderer> results)
        {
            if (!IsDrawable(_bodyRenderer))
            {
                return;
            }

            results.Add(_bodyRenderer);

            if (_headAnchor == null)
            {
                return;
            }

            _headRendererBuffer.Clear();
            _headAnchor.GetComponentsInChildren(false, _headRendererBuffer);
            foreach (var headRenderer in _headRendererBuffer)
            {
                // 体を頭の取り付け位置の下へ置く構成でも、二重に描かない
                if (headRenderer == _bodyRenderer || !IsDrawable(headRenderer))
                {
                    continue;
                }

                results.Add(headRenderer);
            }
        }

        /// <summary>表示中で、メッシュとして取り出せる Renderer か（パーティクルや線は残像にしない）</summary>
        private static bool IsDrawable(Renderer target)
        {
            if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            return target is SkinnedMeshRenderer || target is MeshRenderer;
        }
    }
}
