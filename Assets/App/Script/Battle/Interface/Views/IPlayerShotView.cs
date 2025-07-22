using App.Battle.Data;
using App.Common.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IPlayerShotView
    {
        void SpawnBullet(BulletData bulletData, int focusTargetId);
    }
}