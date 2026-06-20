// このファイルはGASで自動生成されました。手動編集しないでください。

public enum ConditionType
{
    None = 0, // 条件なし（常に効果適用）
    HpBelow = 1, // HP割合がConditionValue未満の間（状態ベース）
    HpAbove = 2, // HP割合がConditionValue以上の間（状態ベース）
    AfterDodge = 3, // 回避直後ConditionValue秒間（タイマー型）
}
