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
            if (EditorPrefs.GetBool("VRMode", false))
            {
                return;
            }

            trackedPoseDriver.enabled = false;
#endif
        }
    }
}