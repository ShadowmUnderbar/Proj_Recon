using System.Collections.Generic;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface
{
    public interface IRunLevelView
    {
        /// <summary>
        /// 選択されたアップグレードのインデックス (0〜choiceCount-1)
        /// </summary>
        Observable<int> OnUpgradeChosen { get; }

        void ShowUpgrades(IReadOnlyList<UpgradeMasterData> options);
        void Hide();
    }
}
