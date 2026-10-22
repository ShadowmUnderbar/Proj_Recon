using UnityEngine;

namespace App.Battle.Views.FloorCollapse
{
    /// <summary>
    /// 床崩壊の対象になる床の目印。バイオームごとの床（見た目用の Renderer）に付ける。
    /// 崩壊中はこの Renderer を隠し、見た目（基本テクスチャ・色）を真上からの投影でプレート床へ写す。
    /// コライダーは別のオブジェクトでもよく、ここでは触れない
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FloorCollapseTargetView : MonoBehaviour
    {
        private Renderer _renderer;

        public Renderer TargetRenderer
        {
            get
            {
                if (_renderer == null)
                {
                    _renderer = GetComponent<Renderer>();
                }

                return _renderer;
            }
        }
    }
}
