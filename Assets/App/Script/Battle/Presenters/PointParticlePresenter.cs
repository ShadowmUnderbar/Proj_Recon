using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class PointParticlePresenter : IPointParticlePresenter
    {
        private readonly IPointParticleStoreView _pointParticleStoreView;

        public Observable<int> OnCollected => _pointParticleStoreView.OnCollected;

        [Inject]
        public PointParticlePresenter(IPointParticleStoreView pointParticleStoreView)
        {
            _pointParticleStoreView = pointParticleStoreView;
        }

        public void Spawn(Vector3 position, IReadOnlyList<PointUnitData> units)
        {
            _pointParticleStoreView.Spawn(position, units);
        }

        public void AllRemove()
        {
            _pointParticleStoreView.AllRemove();
        }
    }
}
