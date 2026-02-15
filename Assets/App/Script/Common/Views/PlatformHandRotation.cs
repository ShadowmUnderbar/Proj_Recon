using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    public class PlatformHandRotation : MonoBehaviour
    {
        public Quaternion Rotation => transform.rotation * AdjustedRotation;
        private static Quaternion AdjustedRotation => DebugConfig.IsVRMode ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;

        private void Awake()
        {
            if (DebugConfig.IsVRMode)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }

            transform.SetParent(Camera.main.transform);
        }
    }
}