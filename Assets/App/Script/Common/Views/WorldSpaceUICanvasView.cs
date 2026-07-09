using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// UnityUIのCanvasをメインカメラに追従させ、World Space化する
    /// VRのハンドレイ（TrackedDeviceGraphicRaycaster）と非VRのマウス（GraphicRaycaster）の
    /// 両方でレイキャスト可能にするための下準備を行う
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class WorldSpaceUICanvasView : MonoBehaviour
    {
        [SerializeField] private Vector3 _localPosition = new(0, 0, 1.2f);
        [SerializeField] private Vector3 _localEulerAngles = Vector3.zero;
        [SerializeField] private float _localScale = 0.001f;

        private Canvas _canvas;
        private bool _isAttached;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            if (_isAttached)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }

            AttachToCamera(Camera.main);
        }

        private void AttachToCamera(Camera targetCamera)
        {
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = targetCamera;

            transform.SetParent(targetCamera.transform, false);
            transform.localPosition = _localPosition;
            transform.localEulerAngles = _localEulerAngles;
            transform.localScale = Vector3.one * _localScale;

            _isAttached = true;
        }
    }
}
