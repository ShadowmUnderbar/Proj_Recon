using App.Common.Data;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace App.Common.Views
{
    /// <summary>
    /// VR向けUI（<see cref="VrUiFollowCanvasView"/>）を操作するための、片手ぶんのハンドレイ。
    /// UI表示中だけ有効化し、XRRayInteractorによるボタン選択と、その見た目のライン描画を行う。
    /// 非VRではマウスでUIを操作するため、常に無効のままにする。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VrUiRayView : MonoBehaviour
    {
        [SerializeField, Tooltip("UI選択に使うレイインタラクター")]
        private XRRayInteractor _rayInteractor;

        [SerializeField, Tooltip("レイの見た目")]
        private LineRenderer _lineRenderer;

        [SerializeField, Tooltip("何にも当たっていないときのレイの長さ[m]")]
        private float _defaultRayLength = 5f;

        private void Awake()
        {
            // UIを開くまではレイを出さない（射撃中に誤ってUIへ干渉しないようにする）
            SetEnable(false);
        }

        /// <summary>UIレイの有効・無効を切り替える。非VRでは常に無効</summary>
        public void SetEnable(bool enable)
        {
            var isEnabled = enable && DebugConfig.IsVRMode;

            if (_rayInteractor != null)
            {
                _rayInteractor.enabled = isEnabled;
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = isEnabled;
            }
        }

        private void LateUpdate()
        {
            if (_lineRenderer == null || !_lineRenderer.enabled || _rayInteractor == null)
            {
                return;
            }

            var origin = _rayInteractor.rayOriginTransform != null
                ? _rayInteractor.rayOriginTransform
                : _rayInteractor.transform;

            _lineRenderer.SetPosition(0, origin.position);
            _lineRenderer.SetPosition(1, GetRayEndPosition(origin));
        }

        /// <summary>レイの終点。UI・3Dのヒット位置を優先し、当たっていなければ既定の長さで描く</summary>
        private Vector3 GetRayEndPosition(Transform origin)
        {
            if (_rayInteractor.TryGetCurrentUIRaycastResult(out var uiResult) && uiResult.isValid)
            {
                return uiResult.worldPosition;
            }

            if (_rayInteractor.TryGetCurrent3DRaycastHit(out var hit))
            {
                return hit.point;
            }

            return origin.position + origin.forward * _defaultRayLength;
        }
    }
}
