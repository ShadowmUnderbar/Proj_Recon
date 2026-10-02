using App.Battle.Interface;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class BossLifeGaugePresenter : IBossLifeGaugePresenter
    {
        private readonly IBossLifeGaugeStoreView _bossLifeGaugeStoreView;

        [Inject]
        public BossLifeGaugePresenter(IBossLifeGaugeStoreView bossLifeGaugeStoreView)
        {
            _bossLifeGaugeStoreView = bossLifeGaugeStoreView;
        }

        public void Add(int enemyId) => _bossLifeGaugeStoreView.Add(enemyId);
        public void Remove(int enemyId) => _bossLifeGaugeStoreView.Remove(enemyId);
        public void SetPosition(int enemyId, Vector3 position) => _bossLifeGaugeStoreView.SetPosition(enemyId, position);
        public void SetHealthRatio(int enemyId, float ratio) => _bossLifeGaugeStoreView.SetHealthRatio(enemyId, ratio);
    }
}
