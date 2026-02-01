using UnityEditor;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace App.Common.Data
{
    public class PlayerCameraData : MonoBehaviour
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