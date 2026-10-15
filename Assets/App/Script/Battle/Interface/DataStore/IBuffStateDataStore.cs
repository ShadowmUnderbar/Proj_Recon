using App.Common.Data;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IBuffStateDataStore
    {
        void AddBuff(BuffMasterData buffData);

        /// <summary>自攻撃が敵にヒットしたことを通知する（HitCount条件の進行・HitDifferentEnemy条件のスタック更新）</summary>
        /// <param name="damagedId">命中した敵のID（HitDifferentEnemyで直前敵との異同判定に使う）</param>
        void NotifyHit(int damagedId);

        /// <summary>
        /// 敵を撃破したことを通知する（KillWithDifferentForm条件の撃破フォーム履歴を更新）
        /// </summary>
        /// <param name="shotType">撃破の決め手となった射撃フォーム。射撃以外の撃破は null（履歴を更新しない）</param>
        void NotifyKill(ShotType? shotType);

        /// <summary>被弾したことを通知する（OnDamaged条件の効果時間を被弾ダメージ量×レベル倍率だけ延長）</summary>
        /// <param name="damage">被弾したダメージ量</param>
        void NotifyDamageTaken(float damage);

        /// <summary>回避が成立したことを通知する（AfterDodge条件の効果時間をDurationでリフレッシュ）</summary>
        void NotifyDodge();

        /// <summary>現在のHP割合（0〜1）を通知する（HpBelow条件の判定）</summary>
        void SetHealthRatio(float healthRatio);

        /// <summary>アクティブなバフの効果値を乗算合成して返す。効果なし時は 1.0f</summary>
        float CalcMultiply(BuffEffectType effectType);

        /// <summary>
        /// 被ダメージに掛ける倍率（軽減バフ）を乗算合成して返す。効果なし時は 1.0f
        /// </summary>
        float CalcDamageTakenMultiply();

        /// <summary>
        /// レイジが発動中か（所持しているレイジ、またはアップグレード無しで与えられたレイジのいずれかが効果時間内か）。
        /// レイジの発動を条件にするアップグレードはここで判定する
        /// </summary>
        bool IsRageActive { get; }

        /// <summary>
        /// レイジの効果時間を得る。発動中なら残り時間に足し、発動していなければその秒数で発動させる。
        /// レイジ未所持なら rageWhenNotOwned のレイジを、被弾では延びない状態で発動させる
        /// （のちにレイジを取得すると、残り時間を引き継いで通常のレイジに置き換わる）
        /// </summary>
        /// <param name="seconds">得る秒数</param>
        /// <param name="rageWhenNotOwned">レイジ未所持のときに発動させるレイジのバフ。null なら未所持では何もしない</param>
        void GainRage(float seconds, BuffMasterData rageWhenNotOwned);
    }
}
