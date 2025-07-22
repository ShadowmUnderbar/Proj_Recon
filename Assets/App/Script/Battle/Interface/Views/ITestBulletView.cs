using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBulletView
    {
        void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId);
    }
}