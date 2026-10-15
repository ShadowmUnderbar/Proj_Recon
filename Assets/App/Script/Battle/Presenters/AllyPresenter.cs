using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class AllyPresenter : IAllyPresenter
    {
        private readonly IAllyStoreView _allyStoreView;

        public Observable<(int allyId, float damage)> OnAllyDamaged => _allyStoreView.OnAllyDamaged;
        public Observable<int> OnShotRequested => _allyStoreView.OnShotRequested;
        public Observable<int> OnAllySpawnFailed => _allyStoreView.OnAllySpawnFailed;

        [Inject]
        public AllyPresenter(IAllyStoreView allyStoreView)
        {
            _allyStoreView = allyStoreView;
        }

        public void Spawn(AllyData allyData)
        {
            _allyStoreView.Spawn(allyData);
        }

        public void UnSpawn(int allyId)
        {
            _allyStoreView.UnSpawn(allyId);
        }

        public void Shot(int allyId, BulletData bulletData)
        {
            _allyStoreView.Shot(allyId, bulletData);
        }

        public void SetPause(bool isPause)
        {
            _allyStoreView.SetPause(isPause);
        }

        public void SetPlayerForward(Vector3 forward)
        {
            _allyStoreView.SetPlayerForward(forward);
        }
    }
}
