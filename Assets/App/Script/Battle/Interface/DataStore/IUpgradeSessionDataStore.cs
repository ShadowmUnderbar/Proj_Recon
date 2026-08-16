using System.Collections.Generic;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeSessionDataStore
    {
        /// <summary>このランで所持している全アップグレードID（読込分＋新規獲得分）。効果計算・抽選除外に使う</summary>
        IReadOnlyList<string> AppliedUpgrades { get; }

        /// <summary>
        /// 所持アップグレードが変化したときに発火する。
        /// 毎フレーム走査したくない効果のキャッシュ更新や、取得直後の表示更新に使う。
        /// </summary>
        Observable<Unit> OnChanged { get; }

        /// <summary>このランで新たに獲得したアップグレードID（セット読込で最初から持っていた分は含まない）。メタ保存の対象</summary>
        IReadOnlyList<string> NewlyAcquiredUpgrades { get; }

        /// <summary>ショップ等で新規に獲得する（新規獲得として記録）</summary>
        void AddUpgrade(UpgradeMasterData upgradeData);

        /// <summary>セット読込でラン開始時に最初から付与する（所持はするが新規獲得には数えない＝保存対象外）</summary>
        void Preload(UpgradeMasterData upgradeData);

        void Reset();
    }
}
