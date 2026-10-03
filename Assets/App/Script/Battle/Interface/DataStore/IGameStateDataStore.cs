using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ラン全体のゲーム状態（ゲームオーバー・クリア）を保持する。
    /// Battleスコープのためラン（シーン）ごとに生成・破棄される。
    /// ゲームオーバーとクリアは排他で、先に入った方だけが有効になる。
    /// </summary>
    public interface IGameStateDataStore
    {
        /// <summary>ゲームオーバーに入ったか</summary>
        ReadOnlyReactiveProperty<bool> IsGameOver { get; }

        /// <summary>クリアしたか</summary>
        ReadOnlyReactiveProperty<bool> IsCleared { get; }

        /// <summary>ゲームオーバーかクリアでランが終わったか</summary>
        bool IsRunEnded { get; }

        /// <summary>ゲームオーバーに遷移する（多重呼び出し・クリア後は無視）</summary>
        void SetGameOver();

        /// <summary>クリアに遷移する（多重呼び出し・ゲームオーバー後は無視）</summary>
        void SetCleared();
    }
}
