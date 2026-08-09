namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 依存ノード（α・β・γ）の実行時状態。
    /// 単体では効果がなく、他アップグレードが「有効な依存ノードの種類数」を参照するための土台。
    /// エマージェンシー・ノード等で無効化された依存ノードは、そのラン中は所持していない扱いになる。
    /// </summary>
    public interface IDependencyNodeDataStore
    {
        /// <summary>所持中かつ無効化されていない依存ノードの種類数</summary>
        int ActiveNodeCount { get; }

        /// <summary>
        /// 有効な依存ノードを1つ無効化する。α→β→γ（マスターデータの並び順）の順で無効化する。
        /// 有効なノードが無ければ false。
        /// </summary>
        bool TryDisableOne(out string disabledNameKey);

        /// <summary>指定IDの依存ノードが無効化済みか</summary>
        bool IsDisabled(string upgradeId);
    }
}
