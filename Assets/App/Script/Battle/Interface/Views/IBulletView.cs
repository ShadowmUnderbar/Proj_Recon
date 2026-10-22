using App.Common.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBulletView
    {
        void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId, Transform targetTransform = null);

        /// <summary>その場で止める／再開する（フリーズ・時止め用）。移動と寿命を止め、当たり判定は生かしたまま</summary>
        void SetPause(bool isPause);
    }
}
