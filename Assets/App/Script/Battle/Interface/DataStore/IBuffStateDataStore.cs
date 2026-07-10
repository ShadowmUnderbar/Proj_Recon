using App.Common.Data;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IBuffStateDataStore
    {
        void AddBuff(BuffMasterData buffData);

        /// <summary>自攻撃が敵にヒットしたことを通知する（HitCount条件の進行）</summary>
        void NotifyHit();

        /// <summary>現在のHP割合（0〜1）を通知する（HpBelow条件の判定）</summary>
        void SetHealthRatio(float healthRatio);

        /// <summary>アクティブなバフの効果値を乗算合成して返す。効果なし時は 1.0f</summary>
        float CalcMultiply(BuffEffectType effectType);

        void Reset();
    }
}
