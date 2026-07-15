// このファイルはGASで自動生成されました。手動編集しないでください。

public enum BuffConditionType
{
    None = 0, // なし
    HpBelow = 1, // HP割合がN未満
    HpAbove = 2, // HP割合がN以上
    AfterDodge = 3, // 回避直後N秒
    HitCount = 4, // ヒット数N到達
    HitDifferentEnemy = 5, // 直前と異なる敵に命中でスタック増加・同一敵でリセット ※手動追加。GAS側スプレッドシートへの反映が必要
}