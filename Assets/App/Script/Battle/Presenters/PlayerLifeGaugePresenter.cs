using App.Battle.Interface;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class PlayerLifeGaugePresenter : IPlayerLifeGaugePresenter
    {
        private readonly IPlayerLifeGaugeView _playerLifeGaugeView;

        [Inject]
        public PlayerLifeGaugePresenter(IPlayerLifeGaugeView playerLifeGaugeView)
        {
            _playerLifeGaugeView = playerLifeGaugeView;
        }

        public void SetPosition(Vector3 position) => _playerLifeGaugeView.SetPosition(position);
        public void SetHealthRatio(float ratio) => _playerLifeGaugeView.SetHealthRatio(ratio);
        public void SetBarrierRatio(float ratio) => _playerLifeGaugeView.SetBarrierRatio(ratio);
        public void SetBarrierVisible(bool visible) => _playerLifeGaugeView.SetBarrierVisible(visible);
    }
}
