using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using App.Battle.Data;
using App.Common.Data;
using App.Common.Data.MasterData;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 詳細説明中の {value1}〜{value5} を、マスターデータの値を表示用に変換して置き換える。
    /// テーブルのエントリは Smart String ではないため、Localization 側の書式化は使わず自前で置換する。
    /// 値の意味（倍率・割合・秒数など）はタイプごとに違うため、変換方法は ValueFormats で個別に定義する
    /// </summary>
    public static class UpgradeDescriptionFormatter
    {
        /// <summary>{valueN} の表示変換方法</summary>
        private enum ValueFormat
        {
            /// <summary>置換しない（説明文で使わない値。{valueN} が残れば定義漏れに気づける）</summary>
            None,

            /// <summary>そのまま表示（回数・秒数・距離・「○倍」表記の倍率）。例: 6 → 6、0.667 → 0.67</summary>
            Raw,

            /// <summary>割合を百分率で表示。例: 0.05 → 5</summary>
            Percent,

            /// <summary>倍率 1.0 からの増減量を百分率の絶対値で表示（増減の向きは文言側で表す）。例: 1.1 → 10、0.95 → 5</summary>
            MultiplierDelta,
        }

        private const int ValueCount = 5;
        private const string PlaceholderFormat = "{{value{0}}}";
        private const string NumberFormat = "0.##";
        private const float PercentScale = 100f;
        private const float MultiplierBase = 1f;

        private static readonly ValueFormat[] MultiplierDeltaValue1 = { ValueFormat.MultiplierDelta };
        private static readonly ValueFormat[] PercentValue1 = { ValueFormat.Percent };
        private static readonly ValueFormat[] RawValue1To2 = { ValueFormat.Raw, ValueFormat.Raw };
        private static readonly ValueFormat[] RawValue1To3 = { ValueFormat.Raw, ValueFormat.Raw, ValueFormat.Raw };

        // タイプごとの Value1〜Value5 の変換方法（配列の添字0=Value1）。
        // 未登録のタイプ・配列の範囲外の値は置換しない。
        // ローカライズ表の説明文（例:「{value1}%アップ」「{value1}倍」）と対応させること
        private static readonly Dictionary<UpgradeType, ValueFormat[]> ValueFormats = new()
        {
            // 基礎値に掛ける倍率（「○%アップ」表記）
            { UpgradeType.BulletDamage, MultiplierDeltaValue1 },
            { UpgradeType.FireRate, MultiplierDeltaValue1 },
            { UpgradeType.HitRange, MultiplierDeltaValue1 },
            { UpgradeType.BombRange, MultiplierDeltaValue1 },
            { UpgradeType.Health, MultiplierDeltaValue1 },
            { UpgradeType.NormalDamage, MultiplierDeltaValue1 },
            { UpgradeType.WaltzDamage, MultiplierDeltaValue1 },
            { UpgradeType.MergeDamage, MultiplierDeltaValue1 },

            // Value1=最大HPに対するバリア量の割合
            { UpgradeType.Barrier, PercentValue1 },

            // Value1=発動確率
            { UpgradeType.ChokePoint, PercentValue1 },
            { UpgradeType.BigMouse, PercentValue1 },
            { UpgradeType.Diversion, PercentValue1 },
            { UpgradeType.LuckyChance, PercentValue1 },
            { UpgradeType.KillingCall, PercentValue1 },
            // Value1=HP全損時のクリティカル確率
            { UpgradeType.TurnTable, PercentValue1 },

            // Value1=マイナー撃破時の回復量 / Value2=メジャー撃破時の回復量
            { UpgradeType.HealOnKill, RawValue1To2 },
            // Value1=連続ノーマルショット数 / Value2=強化CD倍率 / Value3=ペナルティCD倍率
            { UpgradeType.PeaceMaker, RawValue1To3 },
            // Value1=通常CD倍率 / Value2=直前マージ命中時のCD倍率
            { UpgradeType.Avalanche, RawValue1To2 },

            // Value1=速度倍率 / Value2=注視半径(m) / Value3=解除猶予(秒)
            { UpgradeType.SnakeEyes, RawValue1To3 },
            // Value1=スタン秒数 / Value2=注視半径(m)
            { UpgradeType.Medusa, RawValue1To2 },
            // Value1=被ダメージ倍率 / Value2=注視半径(m)
            { UpgradeType.MeanMug, RawValue1To2 },

            // Value1=依存ノード1種あたりの加算率 / Value2=依存ノード0種時の倍率
            { UpgradeType.DamageNode, new[] { ValueFormat.Percent, ValueFormat.Raw } },
            // Value1=依存ノード1種あたりの毎秒回復割合 / Value2=依存ノード0種時の毎秒ダメージ割合 / Value3=最低ダメージ量
            { UpgradeType.CareNode, new[] { ValueFormat.Percent, ValueFormat.Percent, ValueFormat.Raw } },
            // Value1=回復後のHP割合
            { UpgradeType.EmergencyNode, PercentValue1 },

            // Value1=ショップ候補の追加数
            { UpgradeType.Appraisal, new[] { ValueFormat.Raw } },
            // Value2=伝播ダメージ割合（Value1・Value3 は半径計算用で説明文では使わない）
            { UpgradeType.ElectricShock, new[] { ValueFormat.None, ValueFormat.Percent } },

            // Value1=ダメージ倍率 / Value2=クールダウン倍率
            { UpgradeType.ExtraConflict, RawValue1To2 },
            { UpgradeType.FocusConflict, RawValue1To2 },
            { UpgradeType.MergeConflict, RawValue1To2 },
            { UpgradeType.WaltzConflict, RawValue1To2 },
        };

        // 数値と一緒に装飾する直後の単位。「10%」の数字だけ色が変わって単位が浮かないようにまとめて囲む
        private const string UnitSuffixPattern = "(%|倍|秒|m|個|回|x)?";

        // {valueN}（＋直後の単位）を探す正規表現。添字0=value1
        private static readonly Regex[] PlaceholderPatterns = CreatePlaceholderPatterns();

        /// <param name="template">ローカライズ表の詳細説明（{valueN} を含む）</param>
        /// <param name="upgrade">値の埋め込み元</param>
        /// <param name="style">
        /// 値の装飾設定。指定すると ParameterType が強化/弱化の値を色付き（TextMeshPro のリッチテキスト）にする。
        /// null なら装飾せず数値だけを埋め込む
        /// </param>
        public static string Format(string template, UpgradeMasterData upgrade, UpgradeDescriptionStyle style = null)
        {
            if (string.IsNullOrEmpty(template) || !ValueFormats.TryGetValue(upgrade.UpgradeType, out var formats))
            {
                return template;
            }

            var result = template;
            for (var i = 0; i < formats.Length && i < ValueCount; i++)
            {
                if (formats[i] == ValueFormat.None)
                {
                    continue;
                }

                var (value, parameterType) = GetValue(upgrade, i);
                var displayValue = ToDisplayString(value, formats[i]);

                result = PlaceholderPatterns[i].Replace(result, match =>
                    EffectTextStyler.Colorize(displayValue + match.Groups[1].Value, parameterType, style));
            }

            return result;
        }

        private static Regex[] CreatePlaceholderPatterns()
        {
            var patterns = new Regex[ValueCount];
            for (var i = 0; i < ValueCount; i++)
            {
                var placeholder = string.Format(PlaceholderFormat, i + 1);
                patterns[i] = new Regex(Regex.Escape(placeholder) + UnitSuffixPattern, RegexOptions.Compiled);
            }

            return patterns;
        }

        private static (float value, ParameterType parameterType) GetValue(UpgradeMasterData upgrade, int index) => index switch
        {
            0 => upgrade.Value1,
            1 => upgrade.Value2,
            2 => upgrade.Value3,
            3 => upgrade.Value4,
            4 => upgrade.Value5,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };

        private static string ToDisplayString(float value, ValueFormat format)
        {
            var displayValue = format switch
            {
                ValueFormat.Percent => value * PercentScale,
                ValueFormat.MultiplierDelta => Math.Abs(value - MultiplierBase) * PercentScale,
                _ => value
            };
            return displayValue.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
