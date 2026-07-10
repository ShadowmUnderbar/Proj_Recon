// このファイルはGASで自動生成されました。手動編集しないでください。

public enum UpgradeType
{
    None = 0, // なし
    BulletDamage = 1, // ダメージアップ
    FireRate = 2, // 連射速度アップ
    HitRange = 3, // 判定サイズ強化
    BombRange = 4, // 爆破サイズ
    DodgeDistance = 5, // 回避距離アップ
    DodgeCount = 6, // 回避回数アップ
    DodgeCooldown = 7, // 回避クールダウン短縮
    Health = 8, // HP最大値
    GrantBuff = 9, // バフ付与（BuffId列で指定したバフを取得する） ※手動追加。GAS側スプレッドシートへの反映が必要
}