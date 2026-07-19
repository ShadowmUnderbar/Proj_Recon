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

        /// <summary>被弾したことを通知する（OnDamaged条件の効果時間を被弾ダメージ量×レベル倍率だけ延長）</summary>
        /// <param name="damage">被弾したダメージ量</param>
        void NotifyDamageTaken(float damage);

        /// <summary>現在のHP割合（0〜1）を通知する（HpBelow条件の判定）</summary>
        void SetHealthRatio(float healthRatio);

        /// <summary>アクティブなバフの効果値を乗算合成して返す。効果なし時は 1.0f</summary>
        float CalcMultiply(BuffEffectType effectType);

        /// <summary>
        /// PenetrationCount条件バフのダメージ倍率を乗算合成して返す。効果なし時は 1.0f
        /// </summary>
        /// <param name="penetrationIndex">同一弾内で何体目のヒットか（1始まり）</param>
        float CalcPenetrationMultiply(int penetrationIndex);

        void Reset();
    }
}
