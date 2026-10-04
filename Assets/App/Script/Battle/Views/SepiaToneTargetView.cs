using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 自身と子の Renderer を、セピア調のグループに所属させる。
    /// グループは Renderer の renderingLayerMask のビットで表すため、マテリアルを敵・プレイヤーで共有していても分けられる。
    /// 敵・弾・レイ・背景などのプレハブのルートに付ける。uGUI は Renderer を持たないので、UI 用マテリアルの設定で分ける。
    /// </summary>
    public class SepiaToneTargetView : MonoBehaviour
    {
        [SerializeField, Tooltip("所属させるグループ。プリセットで対象に選ばれたグループだけがセピア調になる")]
        private SepiaToneGroup _group = SepiaToneGroup.Enemy;

        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// 所属するグループを変えて付け直す。
        /// プレイヤーのライフゲージをボスのゲージに流用するときのように、同じプレハブを別の陣営で使う場合に呼ぶ
        /// </summary>
        public void SetGroup(SepiaToneGroup group)
        {
            _group = group;
            Apply();
        }

        /// <summary>
        /// 子の Renderer へグループを付け直す。生成後に Renderer を足した場合に呼ぶ。
        /// プールから再利用されるオブジェクトは Renderer の構成が変わらないので、Awake の1回で足りる
        /// </summary>
        public void Apply()
        {
            var layerMask = SepiaToneRenderingLayer.LayerMask(_group);
            var clearMask = ~SepiaToneRenderingLayer.AllLayerBits;

            foreach (var targetRenderer in GetComponentsInChildren<Renderer>(true))
            {
                targetRenderer.renderingLayerMask = (targetRenderer.renderingLayerMask & clearMask) | layerMask;
            }
        }
    }
}
