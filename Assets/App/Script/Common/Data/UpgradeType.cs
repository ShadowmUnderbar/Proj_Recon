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
    Barrier = 10, // バリア（被弾を優先吸収し、無被弾7秒で時間回復する耐久値。Value1=最大HPに対する割合） ※手動追加。GAS側スプレッドシートへの反映が必要
    Diversion = 11, // 陽動（N体撃破すると次の1体がその平均方向からValue1の確率で出現。Lv1=3体/Lv2,3=2体） ※手動追加。GAS側スプレッドシートへの反映が必要
}