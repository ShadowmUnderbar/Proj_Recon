using App.Common.Data;

namespace App.Battle.Interface
{
    public interface IPlayerShotView
    {
        void SpawnBullet(BulletData bulletData, int focusTargetId);
    }
}