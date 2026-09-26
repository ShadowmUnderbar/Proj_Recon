using System.Collections.Generic;

namespace App.Common.Interface
{
    /// <summary>
    /// 次のランで最初から装備するアップグレードセット（ビルド）の選択結果。
    /// メインメニューのSTART後に選び、バトルシーンの RunStartUseCase が読み取って適用する。
    /// シーンをまたいで運ぶため常駐スコープ（CommonLifetimeScope）に置く。
    ///
    /// スロット番号ではなく選んだ時点のID一覧を持つ。ゲームオーバー画面で同じスロットを上書き保存してから
    /// リスタートしても、開始時に選んだセットのまま再開できるようにするため。
    /// </summary>
    public interface IRunLoadoutDataStore
    {
        /// <summary>
        /// メインメニューで選択を済ませているか。
        /// false のとき（エディタでバトルシーンを直接再生した場合など）はバトル側で選択UIを出す。
        /// </summary>
        bool HasSelection { get; }

        /// <summary>選んだセットのアップグレードID一覧。未選択、または「使わずに開始」なら空</summary>
        IReadOnlyList<string> SelectedUpgradeIds { get; }

        /// <summary>アップグレードID一覧を装備して開始する（内容はこの時点でコピーして保持する）</summary>
        void Select(IReadOnlyList<string> upgradeIds);

        /// <summary>セットを使わずに開始する</summary>
        void SelectNone();

        /// <summary>選択を未選択に戻す。メインメニューに入ったときに呼び、前回の選択を持ち越さない</summary>
        void Clear();
    }
}
