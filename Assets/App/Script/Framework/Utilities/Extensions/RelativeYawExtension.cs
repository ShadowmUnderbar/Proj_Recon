using App.Common.Data;
using UnityEngine;

namespace App.Framework.Utilities.Extensions
{
    public abstract class RelativeYawExtension
    {
        public static HitDirectionType GetActorRelative(Pose actorPose, Vector3 targetPosition)
        {
            var forward = actorPose.forward;
            var toTarget = targetPosition - actorPose.position;

            forward.y = 0;
            toTarget.y = 0;

            var dot = forward.x * toTarget.x + forward.z * toTarget.z;

            var magSqrF = forward.x * forward.x + forward.z * forward.z;
            var magSqrT = toTarget.x * toTarget.x + toTarget.z * toTarget.z;

            var threshold = Mathf.Cos(45f * Mathf.Deg2Rad) * Mathf.Sqrt(magSqrF * magSqrT);

            if (dot >= threshold)
            {
                return HitDirectionType.Forward;
            }

            if (dot <= -threshold)
            {
                return HitDirectionType.Backward;
            }

            return HitDirectionType.Side;
        }
    }
}