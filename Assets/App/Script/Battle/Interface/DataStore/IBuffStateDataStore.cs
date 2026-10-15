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

        /// <summary>被弾したことを通知する（レイジ所持なら、レイジの効果時間を被弾ダメージ量×最高レベルの倍率だけ得る）</summary>
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
        /// レイジが発動中か（レイジの効果時間内か）。レイジは何レベル持っていても、獲得元が何でも1つの状態。
        /// レイジの発動を条件にするアップグレードはここで判定する
        /// </summary>
        bool IsRageActive { get; }

        /// <summary>
        /// レイジの残り効果時間（秒）。発動していなければ0
        /// </summary>
        float RageRemainingTime { get; }

        /// <summary>
        /// レイジの効果時間を得る。発動中なら残り時間に足し、発動していなければその秒数で発動させる。
        /// レイジ未所持なら rageWhenNotOwned のバフの効果で発動させる（レイジのアップグレードの効果である「被弾で延びる」は付かない）
        /// </summary>
        /// <param name="seconds">得る秒数</param>
        /// <param name="rageWhenNotOwned">レイジ未所持のときに効果に使うレイジのバフ。レイジ所持中は使わない</param>
        void GainRage(float seconds, BuffMasterData rageWhenNotOwned);
    }
}
