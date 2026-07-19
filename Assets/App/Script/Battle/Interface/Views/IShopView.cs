using System.Collections.Generic;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface
{
    public interface IShopView
    {
        /// <summary>アップグレードボタン押下（押されたボタンのインデックス）</summary>
        Observable<int> OnUpgradeSelected { get; }

        /// <summary>次のウェーブへボタン押下</summary>
        Observable<Unit> OnNextWavePressed { get; }

        void Open(IReadOnlyList<UpgradeMasterData> upgrades);

        /// <summary>指定インデックスのアップグレードボタンだけを非表示にする（他の候補は表示したまま）</summary>
        void HideUpgradeButton(int index);

        void Close();
    }
}
