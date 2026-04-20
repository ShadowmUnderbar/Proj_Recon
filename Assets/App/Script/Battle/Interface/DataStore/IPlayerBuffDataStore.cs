using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerBuffDataStore
    {
        /// <summary>バフが付与・変化・除去されたときに発火。</summary>
        Observable<BuffType> OnBuffChanged { get; }

        IReadOnlyList<ActiveBuffData> ActiveBuffs { get; }

        /// <summary>バフを付与する（スタック加算含む）。</summary>
        void AddBuff(BuffMasterData masterData);

        /// <summary>指定IDのバフを手動除去する（HasDuration=falseのバフ向け）。</summary>
        bool RemoveBuff(string buffId);

        /// <summary>全バフをクリアする（バトル終了時など）。</summary>
        void ClearAllBuffs();

        /// <summary>
        /// 指定バフ種別の最終倍率を返す。
        /// 未付与時は1.0f。FireRateは大きいほど連射が速い（クールダウン除算で使用）。
        /// </summary>
        float GetEffectMultiplier(BuffType buffType);
    }
}
