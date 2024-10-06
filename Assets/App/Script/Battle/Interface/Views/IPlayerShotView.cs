using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IPlayerShotView
    {
        Observable<HitData> OnHit { get; }

        void SpawnBullet();
    }
}