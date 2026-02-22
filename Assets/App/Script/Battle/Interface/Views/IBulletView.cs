using App.Common.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBulletView
    {
        void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId, Transform targetTransform = null);
    }
}