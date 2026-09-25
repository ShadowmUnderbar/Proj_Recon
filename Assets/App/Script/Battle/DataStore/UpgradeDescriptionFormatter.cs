using System;
using System.Collections.Generic;
using System.Globalization;
using App.Common.Data.MasterData;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 詳細説明中の {valueN} をマスターデータの値で置き換える。
    /// テーブルのエントリは Smart String ではないため、Localization 側の書式化は使わず自前で置換する
    /// </summary>
    internal static class UpgradeDescriptionFormatter
    {
        private const string Value1Placeholder = "{value1}";
        private const string NumberFormat = "0.#";
        private const float PercentScale = 100f;

        // 倍率 1.0 を基準とした増減量（1.1 → 10、0.95 → 5）
        private const float MultiplierBase = 1f;

        // Value1 が「基礎値に掛ける倍率」であるタイプ（CalcMultiply で消費される）。
        // それ以外（割合・秒数・回数など）は値の意味がタイプごとに違うため、変換規則が決まるまで置換しない。
        // 誤った数値を黙って出すより、{valueN} がそのまま見える方が不備に気づきやすい
        private static readonly HashSet<UpgradeType> MultiplierValue1Types = new()
        {
            UpgradeType.BulletDamage,
            UpgradeType.FireRate,
            UpgradeType.HitRange,
            UpgradeType.BombRange,
            UpgradeType.Health,
            UpgradeType.NormalDamage,
            UpgradeType.WaltzDamage,
            UpgradeType.MergeDamage,
        };

        public static string Format(string template, UpgradeMasterData upgrade)
        {
            if (string.IsNullOrEmpty(template) || !MultiplierValue1Types.Contains(upgrade.UpgradeType))
            {
                return template;
            }

            return template.Replace(Value1Placeholder, ToPercentDelta(upgrade.Value1.value));
        }

        // 倍率タイプの説明文は「{value1}%」＋増減の向きを文言側で表現する形のため、増減率(%)の絶対値で埋める
        private static string ToPercentDelta(float multiplier)
        {
            var percent = Math.Abs(multiplier - MultiplierBase) * PercentScale;
            return percent.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
