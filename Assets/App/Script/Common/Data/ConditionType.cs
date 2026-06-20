// パッシブ効果の発動条件を表す。
// ConditionValue の意味は条件ごとに異なる点に注意（コメント参照）。
public enum ConditionType
{
    None = 0,       // 条件なし＝従来の恒久アップグレード（常に効果適用）
    HpBelow = 1,    // HP割合が ConditionValue 未満の「間」効果適用（状態ベース。ConditionValue=0〜1の割合）
    HpAbove = 2,    // HP割合が ConditionValue 以上の「間」効果適用（状態ベース。ConditionValue=0〜1の割合）
    AfterDodge = 3, // 回避直後 ConditionValue 秒間だけ効果適用（タイマー型。ConditionValue=継続秒数）
}
