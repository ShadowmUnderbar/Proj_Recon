using System.Text.RegularExpressions;
using App.Battle.Data;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 強化/弱化効果の文字色を付けるための共通処理（TextMeshPro／uGUI のリッチテキスト）。
    /// ・効果値を ParameterType に応じた色で囲む（<see cref="Colorize"/>）
    /// ・文言中の独自タグ &lt;p&gt;…&lt;/p&gt;（強化色）/ &lt;n&gt;…&lt;/n&gt;（弱化色）を色タグへ置き換える（<see cref="ApplyEffectTags"/>）
    /// どちらも <see cref="UpgradeDescriptionStyle"/> の色を使う。ローカライズ文言に限らず任意のテキストに使える
    /// </summary>
    public static class EffectTextStyler
    {
        // 開き・閉じが対になったものだけを置き換える（閉じ忘れは置換せず文字のまま残し、表示で気づけるようにする）。
        // TextMeshPro の <page> / <pos=…> / <nobr> 等を巻き込まないよう、タグ名は完全一致で探す
        private static readonly Regex PositiveTagPattern = new("<p>(.*?)</p>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex NegativeTagPattern = new("<n>(.*?)</n>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// テキストを効果の種別に応じた色で囲む。None・装飾設定なし（style=null）はそのまま返す
        /// </summary>
        public static string Colorize(string text, ParameterType parameterType, UpgradeDescriptionStyle style)
        {
            var colorCode = GetColorCode(parameterType, style);
            return colorCode == null ? text : $"<color={colorCode}>{text}</color>";
        }

        /// <summary>
        /// &lt;p&gt;…&lt;/p&gt; を強化色、&lt;n&gt;…&lt;/n&gt; を弱化色の色タグへ置き換える。
        /// 装飾設定なし（style=null）の場合は、タグだけ外して中身の文字をそのまま残す
        /// </summary>
        public static string ApplyEffectTags(string text, UpgradeDescriptionStyle style)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            {
                return text;
            }

            var result = PositiveTagPattern.Replace(text, match => Colorize(match.Groups[1].Value, ParameterType.Positive, style));
            return NegativeTagPattern.Replace(result, match => Colorize(match.Groups[1].Value, ParameterType.Negative, style));
        }

        // 強化/弱化なら色コード（#RRGGBBAA）、それ以外（None・装飾設定なし）は null＝装飾しない
        private static string GetColorCode(ParameterType parameterType, UpgradeDescriptionStyle style)
        {
            if (style == null)
            {
                return null;
            }

            return parameterType switch
            {
                ParameterType.Positive => "#" + ColorUtility.ToHtmlStringRGBA(style.PositiveColor),
                ParameterType.Negative => "#" + ColorUtility.ToHtmlStringRGBA(style.NegativeColor),
                _ => null
            };
        }
    }
}
