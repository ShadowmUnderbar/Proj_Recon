using System.Collections.Generic;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface
{
    public interface IRunLevelPresenter
    {
        Observable<UpgradeMasterData> OnUpgradeSelected { get; }
        void ShowUpgradeSelection(IReadOnlyList<UpgradeMasterData> options);
        void HideUpgradeSelection();
    }
}
