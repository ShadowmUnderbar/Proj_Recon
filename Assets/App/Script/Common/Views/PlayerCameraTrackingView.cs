using App.Common.Data;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace App.Common.Views
{
    /// <summary>
    /// プレイヤーカメラのHMD追従を、エディタの非VRモードでは切る。
    /// 製品ビルドでは常にVRモードなので何もしない。
    /// </summary>
    public class PlayerCameraTrackingView : MonoBehaviour
    {
        [SerializeField] private TrackedPoseDriver trackedPoseDriver;

        private void Awake()
        {
#if UNITY_EDITOR
            if (DebugConfig.IsVRMode)
            {
                return;
            }

            trackedPoseDriver.enabled = false;
#endif
        }
    }
}
