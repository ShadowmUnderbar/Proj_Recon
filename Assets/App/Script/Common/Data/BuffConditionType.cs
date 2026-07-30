// このファイルはGASで自動生成されました。手動編集しないでください。

public enum BuffConditionType
{
    None = 0, // なし
    HpBelow = 1, // HP割合がN未満
    HpAbove = 2, // HP割合がN以上
    AfterDodge = 3, // 回避直後N秒
    HitCount = 4, // ヒット数N到達
    HitDifferentEnemy = 5, // 直前と異なる敵に命中でスタック増加・同一敵でリセット
    PenetrationCount = 6, // 1発の弾がN体貫通するごとにダメージ倍率上昇
    OnDamaged = 7, // 被弾するとダメージ量×倍率だけ効果時間が延長
    KillWithDifferentForm = 8, // 直前と異なるフォームでの撃破でスタック増加・同一フォームでリセット
    HpLossScaling = 9, // HP減少割合に比例して効果が増加（常時発動）
}