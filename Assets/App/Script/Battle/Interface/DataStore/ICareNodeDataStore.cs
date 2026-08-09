namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ケア・ノードの効果計算。
    /// 有効な依存ノードの種類数に応じて毎秒HPが回復し、依存ノードを持たない場合は逆に毎秒ダメージを受ける。
    /// </summary>
    public interface ICareNodeDataStore
    {
        /// <summary>毎秒の回復量。ケア・ノード未所持、または有効な依存ノードが0なら false。</summary>
        bool TryGetHealPerSecond(float maxHealth, out float amount);

        /// <summary>毎秒のデメリットダメージ量。ケア・ノード未所持、または有効な依存ノードが1つ以上あれば false。</summary>
        bool TryGetDamagePerSecond(float maxHealth, out float amount);
    }
}
