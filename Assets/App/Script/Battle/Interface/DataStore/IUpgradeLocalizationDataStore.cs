using App.Battle.Data;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// アップグレードのローカライズ文言（Localization の "Upgrade" テーブル）を提供する
    /// </summary>
    public interface IUpgradeLocalizationDataStore
    {
        /// <summary>現在ロケールのテーブルを読み込み済みか。false の間は GetText がキー文字列を返す</summary>
        bool IsReady { get; }

        /// <summary>
        /// テーブルの読込完了時・ロケール切替後の再読込完了時（失敗して IsReady=false になった場合も含む）に発火する。
        /// ロケール切替の再読込中は、完了するまで前ロケールの文言を返す。
        /// 表示側はこれを購読して文言を取り直す
        /// </summary>
        Observable<Unit> OnTableChanged { get; }

        /// <summary>アップグレードのタイトル・簡略説明・詳細説明・レベル表記を取得する</summary>
        UpgradeLocalizedText GetText(UpgradeMasterData upgrade);
    }
}
