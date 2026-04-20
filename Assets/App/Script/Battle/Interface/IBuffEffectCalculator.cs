using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data.MasterData;

namespace App.Battle.Interface
{
    /// <summary>
    /// バフ効果量計算のインターフェース。
    /// BuffTypeごとに個別実装を作成し、BattleLifetimeScopeでコレクション登録する。
    /// </summary>
    public interface IBuffEffectCalculator
    {
        /// <summary>このCalculatorが対応するバフ種別。</summary>
        BuffType TargetBuffType { get; }

        /// <summary>
        /// アクティブバフリストから最終倍率を計算して返す。
        /// 未付与または対象バフなしの場合は1.0fを返す。
        /// 1.0fより大きい = 強化、小さい = 弱体化。
        /// </summary>
        float Calculate(
            IReadOnlyList<ActiveBuffData> activeBuffs,
            Func<string, BuffMasterData> masterDataResolver
        );
    }
}
