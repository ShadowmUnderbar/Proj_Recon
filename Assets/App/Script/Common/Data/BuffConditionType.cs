// このファイルはGASで自動生成されました。手動編集しないでください。

public enum BuffConditionType
{
    None = 0, // なし
    HpBelow = 1, // HP割合がN未満
    HpAbove = 2, // HP割合がN以上
    AfterDodge = 3, // 回避直後N秒
    HitCount = 4, // ヒット数N到達
    HitDifferentEnemy = 5, // 直前と異なる敵に命中でスタック増加・同一敵でリセット ※手動追加。スプレッドシート反映済み（次回GASエクスポートで正式生成される）
    PenetrationCount = 6, // 1発の弾がN体貫通するごとにダメージ倍率上昇 ※手動追加。スプレッドシート反映済み（次回GASエクスポートで正式生成される）
}