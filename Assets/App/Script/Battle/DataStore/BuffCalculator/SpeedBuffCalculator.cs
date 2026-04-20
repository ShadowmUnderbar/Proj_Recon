using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data.MasterData;

namespace App.Battle.DataStore
{
    /// <summary>Speedバフ効果量計算。スタック数分のBaseValueを乗算合成する。</summary>
    public class SpeedBuffCalculator : IBuffEffectCalculator
    {
        public BuffType TargetBuffType => BuffType.Speed;

        public float Calculate(
            IReadOnlyList<ActiveBuffData> activeBuffs,
            Func<string, BuffMasterData> masterDataResolver)
        {
            var result = 1f;
            foreach (var buff in activeBuffs)
            {
                if (buff.BuffType != BuffType.Speed) continue;
                var master = masterDataResolver(buff.BuffId);
                if (master == null) continue;
                for (var i = 0; i < buff.StackCount; i++)
                    result *= 1f + master.BaseValue;
            }
            return result;
        }
    }
}
