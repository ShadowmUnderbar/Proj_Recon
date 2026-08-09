namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// エマージェンシー・ノードの効果判定。
    /// 致死ダメージを受けた際、有効な依存ノードを1つ無効化することでそのダメージを無効化しHPを回復する。
    /// </summary>
    public interface IEmergencyNodeDataStore
    {
        /// <summary>
        /// 復活を試みる。成功した場合は依存ノードを1つ消費し、回復割合（最大HPに対する比率）を返す。
        /// エマージェンシー・ノード未所持、または有効な依存ノードが無ければ false。
        /// </summary>
        bool TryActivate(out float healRatio);
    }
}
