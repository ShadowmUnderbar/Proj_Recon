using UnityEngine;

namespace App.Common.Views
{
    public class PlatformHandRotation : MonoBehaviour
    {
        public Quaternion Rotation => transform.rotation * AdjustedRotation;
        private static Quaternion AdjustedRotation => Quaternion.Euler(90, 0, 0);
    }
}