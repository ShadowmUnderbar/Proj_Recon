using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface
{
    public interface IShopPresenter
    {
        /// <summary>アップグレードボタン押下（押されたボタンのインデックス）</summary>
        Observable<int> OnUpgradeSelected { get; }

        /// <summary>次のウェーブへボタン押下</summary>
        Observable<Unit> OnNextWavePressed { get; }

        void Open(IReadOnlyList<UpgradeMasterData> upgrades);

        /// <summary>指定インデックスのアップグレードボタンだけを非表示にする（購入済みの候補に使う）</summary>
        void HideUpgradeButton(int index);

        /// <summary>所持ポイントの表示を更新する</summary>
        void SetCurrentPoint(int currentPoint);

        /// <summary>
        /// 指定インデックスの候補を購入できるか（ポイントが足りるか）を反映する。
        /// 買えない候補はボタンを押せなくする
        /// </summary>
        void SetPurchasable(int index, bool isPurchasable);

        /// <summary>3Dカードの掴み・確定判定に使う片手ぶんの入力を渡す（VRのみ）</summary>
        void UpdateHandInput(in ShopHandInput input);

        void Close();
    }
}
