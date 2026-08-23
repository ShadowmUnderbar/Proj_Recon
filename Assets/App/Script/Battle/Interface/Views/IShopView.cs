using System.Collections.Generic;
using App.Battle.Data;
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

        /// <summary>指定インデックスのアップグレード候補だけを非表示にする（他の候補は表示したまま）</summary>
        void HideUpgradeButton(int index);

        /// <summary>
        /// 3Dカードの掴み・確定判定に使う片手ぶんの入力を渡す（VRのみ。それ以外では無視される）
        /// </summary>
        void UpdateHandInput(in ShopHandInput input);

        void Close();
    }
}
