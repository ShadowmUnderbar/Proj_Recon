using App.Common.Data;
using R3;

namespace App.Common.Interface
{
    /// <summary>
    /// チュートリアルのローカライズ文言（Localization の "TutorialText" テーブル）を提供する
    /// </summary>
    public interface ITutorialLocalizationDataStore
    {
        /// <summary>現在ロケールのテーブルを読み込み済みか。false の間は GetText がキー文字列を返す</summary>
        bool IsReady { get; }

        /// <summary>
        /// テーブルの読込完了時・ロケール切替後の再読込完了時（失敗して IsReady=false になった場合も含む）に発火する。
        /// 表示側はこれを購読して文言を取り直す
        /// </summary>
        Observable<Unit> OnTableChanged { get; }

        /// <summary>指定チュートリアルの本文を取得する。未読込・未登録キーのときはキー文字列を返す</summary>
        string GetText(TutorialType type);
    }
}
