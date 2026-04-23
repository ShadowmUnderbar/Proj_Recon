using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Common.Data.MasterData;
using R3;
using VContainer;

namespace App.Battle.Presenters
{
    public class RunLevelPresenter : IRunLevelPresenter, IDisposable
    {
        private readonly IRunLevelView _view;
        private readonly Subject<UpgradeMasterData> _onUpgradeSelected = new();
        private readonly CompositeDisposable _disposable = new();

        private IReadOnlyList<UpgradeMasterData> _currentOptions = Array.Empty<UpgradeMasterData>();

        public Observable<UpgradeMasterData> OnUpgradeSelected => _onUpgradeSelected;

        [Inject]
        public RunLevelPresenter(IRunLevelView view)
        {
            _view = view;

            _view.OnUpgradeChosen
                .Where(i => i >= 0 && i < _currentOptions.Count)
                .Select(i => _currentOptions[i])
                .Subscribe(_onUpgradeSelected.OnNext)
                .AddTo(_disposable);
        }

        public void ShowUpgradeSelection(IReadOnlyList<UpgradeMasterData> options)
        {
            _currentOptions = options;
            _view.ShowUpgrades(options);
        }

        public void HideUpgradeSelection() => _view.Hide();

        public void Dispose()
        {
            _disposable?.Dispose();
            _onUpgradeSelected?.Dispose();
        }
    }
}
