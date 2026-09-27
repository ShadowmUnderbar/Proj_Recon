using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    public class PlatformHandRotation : MonoBehaviour
    {
        public Quaternion Rotation => transform.rotation * PointingAdjustment;

        /// <summary>
        /// コントローラのTransformから「指し示している向き」へ直す補正。
        /// VRのコントローラは前方が上向きに寝ているため、90度起こしたものを前方として扱う
        /// </summary>
        public static Quaternion PointingAdjustment =>
            DebugConfig.IsVRMode ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;

        /// <summary>コントローラの生の姿勢を、実際に指し示している向き（ハンドレイと同じ基準）へ直す</summary>
        public static Pose ToPointingPose(Pose handPose)
        {
            return new Pose(handPose.position, handPose.rotation * PointingAdjustment);
        }

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
