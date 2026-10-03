using System.Collections.Generic;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ランの結果画面に出す「獲得アップグレードの一覧」の文字列を組み立てる。
    /// </summary>
    public interface IAcquiredUpgradeListBuilder
    {
        /// <summary>
        /// 獲得したアップグレードIDの並び（獲得順）から、「・名前-レベル」を半角スペースで区切って並べた一覧を作る。
        /// 件数が増えても縦に伸びないよう、表示側で横に折り返す前提。
        /// 同じIDは重ねて持てない（UpgradeSessionDataStore が弾く）ため、レベル違いは別の項目になる。空なら「なし」
        /// </summary>
        string Build(IReadOnlyList<string> upgradeIds);
    }
}
