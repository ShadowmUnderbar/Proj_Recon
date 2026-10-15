using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IAllyPresenter
    {
        Observable<(int allyId, float damage)> OnAllyDamaged { get; }
        Observable<int> OnShotRequested { get; }
        Observable<int> OnAllySpawnFailed { get; }

        void Spawn(AllyData allyData);
        void UnSpawn(int allyId);
        void Shot(int allyId, BulletData bulletData);
        void SetPause(bool isPause);
        void SetPlayerForward(Vector3 forward);
    }
}
