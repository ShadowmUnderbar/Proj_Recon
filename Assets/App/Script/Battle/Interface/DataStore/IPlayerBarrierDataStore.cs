using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// プレイヤーのバリア（被弾を優先吸収する耐久値）の実行時状態を管理する。
    /// バリアが残っていれば被弾ダメージを全て肩代わりし、無被弾が続くと時間で回復する。
    /// </summary>
    public interface IPlayerBarrierDataStore
    {
        /// <summary>現在のバリア残量</summary>
        ReadOnlyReactiveProperty<float> CurrentBarrier { get; }

        /// <summary>バリア最大値（0ならバリア未取得）</summary>
        ReadOnlyReactiveProperty<float> MaxBarrier { get; }

        /// <summary>
        /// 被弾ダメージをバリアで吸収する。残量が1以上あれば攻撃を全て吸収し（超過分も破棄し）true を返す。
        /// 残量0なら何もせず false を返す（＝HP側で処理する）。
        /// </summary>
        bool TryAbsorb(float damage);

        /// <summary>被弾を通知して回復待機タイマーをリセットする（吸収の有無に関わらず呼ぶ）。</summary>
        void NotifyDamaged();

        /// <summary>
        /// バリアアップグレード取得時に呼ぶ。最大HPと取得済みバリア倍率から最大値を再計算し、満タンにする。
        /// </summary>
        void GrantFull(float maxHealth);

        /// <summary>状態を初期化する。</summary>
        void Reset();
    }
}
