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
        /// 買えない候補はボタンを押せなくし、3Dカードのときは灰色にして確定させない
        /// </summary>
        void SetPurchasable(int index, bool isPurchasable);

        /// <summary>
        /// 指定インデックスの候補にローカライズ済みの文言を反映する。
        /// Open の後に呼ぶ。ロケール切替などで文言が変わったときも呼び直される
        /// </summary>
        void SetUpgradeText(int index, in UpgradeLocalizedText text);

        /// <summary>3Dカードの掴み・確定判定に使う片手ぶんの入力を渡す（VRのみ）</summary>
        void UpdateHandInput(in ShopHandInput input);

        /// <summary>3Dカードのクリック判定に使うポインタ入力を渡す（非VRのみ）</summary>
        void UpdatePointerInput(in ShopPointerInput input);

        void Close();
    }
}
