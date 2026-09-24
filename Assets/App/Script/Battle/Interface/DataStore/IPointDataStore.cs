using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ランを通して持ち歩くポイント（ショップ通貨）の保持。
    /// </summary>
    public interface IPointDataStore
    {
        /// <summary>現在の所持ポイント</summary>
        ReadOnlyReactiveProperty<int> CurrentPoint { get; }

        /// <summary>粒子を回収したときのポイント加算（0以下は無視する）</summary>
        void Add(int amount);

        /// <summary>
        /// ポイントを消費する。足りなければ何もせず false を返す（0以下のコストは無償として true）
        /// </summary>
        bool TrySpend(int amount);
    }
}
