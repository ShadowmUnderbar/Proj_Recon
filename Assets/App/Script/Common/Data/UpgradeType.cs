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
    NormalDamage = 11, // ノーマル弾ダメージアップ（Normalフォームのみに乗算） ※手動追加。GAS側スプレッドシートへの反映が必要
    WaltzDamage = 12, // ワルツ弾ダメージアップ（Waltzフォームのみに乗算） ※手動追加。GAS側スプレッドシートへの反映が必要
    MergeDamage = 13, // マージ弾ダメージアップ（Mergeフォームのみに乗算） ※手動追加。GAS側スプレッドシートへの反映が必要
    ChokePoint = 14, // チョークポイント（敵の出現位置を直前の敵の付近に寄せる。Value1=寄せ確率、Lv3で次の1体も同位置） ※手動追加。GAS側スプレッドシートへの反映が必要
    BigMouse = 15, // ビッグマウス（マイナー枠のスポーンをValue1の確率で置換。Lv1/2=MinorRush、Lv3=CommonRush） ※手動追加。GAS側スプレッドシートへの反映が必要
}