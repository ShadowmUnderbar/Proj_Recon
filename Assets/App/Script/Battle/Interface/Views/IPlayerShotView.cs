using App.Common.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerShotView
    {
        void SpawnBullet(BulletData bulletData, int focusTargetId);

        /// <summary>指定座標へ向けて弾を発射する（発射位置は同じ、向きだけ対象に合わせる）</summary>
        void SpawnBulletToward(BulletData bulletData, int focusTargetId, Vector3 targetPosition);
    }
}