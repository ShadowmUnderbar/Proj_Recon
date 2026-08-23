using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data.MasterData;
using R3;
using VContainer;

namespace App.Battle.Presenters
{
    public class ShopPresenter : IShopPresenter
    {
        private readonly IShopView _shopView;

        public Observable<int> OnUpgradeSelected => _shopView.OnUpgradeSelected;
        public Observable<Unit> OnNextWavePressed => _shopView.OnNextWavePressed;

        [Inject]
        public ShopPresenter(
            IShopView shopView
        )
        {
            _shopView = shopView;
        }

        public void Open(IReadOnlyList<UpgradeMasterData> upgrades)
        {
            _shopView.Open(upgrades);
        }

        public void HideUpgradeButton(int index)
        {
            _shopView.HideUpgradeButton(index);
        }

        public void UpdateHandInput(in ShopHandInput input)
        {
            _shopView.UpdateHandInput(input);
        }

        public void Close()
        {
            _shopView.Close();
        }
    }
}
