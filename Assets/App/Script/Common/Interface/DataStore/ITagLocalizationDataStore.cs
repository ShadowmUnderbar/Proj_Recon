using App.Common.Data;
using R3;

namespace App.Common.Interface
{
    /// <summary>
    /// アップグレードのタグ名（Localization の "TagText" テーブル）を提供する
    /// </summary>
    public interface ITagLocalizationDataStore
    {
        /// <summary>現在ロケールのテーブルを読み込み済みか。false の間は GetName がキー文字列を返す</summary>
        bool IsReady { get; }

        /// <summary>
        /// テーブルの読込完了時・ロケール切替後の再読込完了時（失敗して IsReady=false になった場合も含む）に発火する。
        /// 表示側はこれを購読して名前を取り直す
        /// </summary>
        Observable<Unit> OnTableChanged { get; }

        /// <summary>タグの表示名を取得する。未読込・未登録キーのときはキー文字列を返す</summary>
        string GetName(UpgradeTag tag);
    }
}
