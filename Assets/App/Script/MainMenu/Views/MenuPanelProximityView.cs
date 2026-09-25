using UnityEngine;

namespace App.MainMenu.Views
{
    /// <summary>
    /// 部屋に固定設置したUIパネルを、プレイヤーが近づいたときだけ操作できるようにする。
    /// どこからでもハンドレイが通ると、歩いている最中に反対側のパネルを誤って押してしまうため、
    /// 一定距離まで寄ったときだけレイキャストを通し、離れている間は減光して操作不可にする。
    ///
    /// 距離の判定にはヒステリシスを持たせ、境界上で操作可否がちらつかないようにしている。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuPanelProximityView : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("この距離[m]まで近づくと操作できるようになる")]
        private float _activateDistance = 2.5f;

        [SerializeField, Min(0f), Tooltip("この距離[m]まで離れると操作できなくなる。_activateDistanceより大きくすること")]
        private float _deactivateDistance = 3f;

        [SerializeField, Range(0f, 1f), Tooltip("操作できないときの不透明度")]
        private float _inactiveAlpha = 0.35f;

        [SerializeField, Min(0f), Tooltip("不透明度の変化速度[/s]")]
        private float _fadeSpeed = 4f;

        private CanvasGroup _canvasGroup;

        /// <summary>距離判定の基準にするカメラ（VRではHMD）。毎フレーム取得しないようキャッシュする</summary>
        private Camera _targetCamera;

        /// <summary>現在操作できる状態か</summary>
        private bool _isActive;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            ApplyActive(false);
            _canvasGroup.alpha = _inactiveAlpha;
        }

        private void Update()
        {
            if (!TryGetTargetCamera(out var targetCamera))
            {
                return;
            }

            var distance = Vector3.Distance(targetCamera.transform.position, transform.position);
            var threshold = _isActive ? _deactivateDistance : _activateDistance;
            var isActive = distance <= threshold;

            if (isActive != _isActive)
            {
                ApplyActive(isActive);
            }

            var targetAlpha = _isActive ? 1f : _inactiveAlpha;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, _fadeSpeed * Time.deltaTime);
        }

        private void ApplyActive(bool isActive)
        {
            _isActive = isActive;
            _canvasGroup.interactable = isActive;
            _canvasGroup.blocksRaycasts = isActive;
        }

        /// <summary>基準カメラを取得する。シーン開始直後はまだ生成されていないことがあるため都度試す</summary>
        private bool TryGetTargetCamera(out Camera targetCamera)
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            targetCamera = _targetCamera;

            return targetCamera != null;
        }
    }
}
