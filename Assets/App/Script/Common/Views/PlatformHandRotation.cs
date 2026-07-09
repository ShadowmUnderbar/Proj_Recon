using App.Common.Data;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace App.Common.Views
{
    public class PlatformHandRotation : MonoBehaviour
    {
        [SerializeField, Tooltip("UIボタン選択用のハンドレイ（VR時のみ有効化）")]
        private XRBaseInteractor _uiRayInteractor;

        public Quaternion Rotation => transform.rotation * AdjustedRotation;
        private static Quaternion AdjustedRotation => DebugConfig.IsVRMode ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;

        private void Awake()
        {
            if (DebugConfig.IsVRMode)
            {
                return;
            }

            if (_uiRayInteractor != null)
            {
                _uiRayInteractor.enabled = false;
            }

            if (Camera.main == null)
            {
                return;
            }

            transform.SetParent(Camera.main.transform);
        }
    }
}