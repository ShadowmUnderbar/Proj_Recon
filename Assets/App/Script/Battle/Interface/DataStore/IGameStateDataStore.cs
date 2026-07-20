using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ラン全体のゲーム状態（ゲームオーバーなど）を保持する。
    /// Battleスコープのためラン（シーン）ごとに生成・破棄される。
    /// </summary>
    public interface IGameStateDataStore
    {
        /// <summary>ゲームオーバーに入ったか</summary>
        ReadOnlyReactiveProperty<bool> IsGameOver { get; }

        /// <summary>ゲームオーバーに遷移する（多重呼び出しは無視）</summary>
        void SetGameOver();
    }
}
