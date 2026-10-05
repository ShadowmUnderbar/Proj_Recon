using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// 文言に文字色を付けるための共通処理（TextMeshPro／uGUI のリッチテキスト）。
    /// ・効果値を ParameterType に応じた色で囲む（<see cref="Colorize"/>）
    /// ・文言中の独自タグ（&lt;p&gt;…&lt;/p&gt; など）を色タグへ置き換える（<see cref="ApplyEffectTags"/>）
    /// どちらも <see cref="EffectTextStyle"/> に登録したタグと色の対応を使う。ローカライズ文言に限らず任意のテキストに使える
    /// </summary>
    public static class EffectTextStyler
    {
        // 効果値の色に使うタグ名（EffectTextStyle に登録した色を引く）
        private const string PositiveTagName = "p";
        private const string NegativeTagName = "n";

        // 入れ子の置き換えを繰り返す上限（外側を置き換えると内側のタグが残るため、変化がなくなるまで繰り返す）
        private const int MaxNestDepth = 8;

        /// <summary>
        /// テキストを効果の種別に応じた色で囲む。None・装飾設定なし（style=null）・色が未登録ならそのまま返す
        /// </summary>
        public static string Colorize(string text, ParameterType parameterType, EffectTextStyle style)
        {
            var tagName = parameterType switch
            {
                ParameterType.Positive => PositiveTagName,
                ParameterType.Negative => NegativeTagName,
                _ => null
            };

            if (style == null || tagName == null || !style.TryGetColor(tagName, out var color))
            {
                return text;
            }

            return WrapColor(text, color);
        }

        /// <summary>
        /// EffectTextStyle に登録したタグ（&lt;p&gt;…&lt;/p&gt; など）を、対応する色の色タグへ置き換える。
        /// 装飾設定なし（style=null）の場合はそのまま返す
        /// </summary>
        public static string ApplyEffectTags(string text, EffectTextStyle style)
        {
            var pattern = style != null ? style.TagPattern : null;
            if (pattern == null || string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            {
                return text;
            }

            var result = text;
            for (var i = 0; i < MaxNestDepth; i++)
            {
                var replaced = pattern.Replace(result, match =>
                    style.TryGetColor(match.Groups[1].Value, out var color)
                        ? WrapColor(match.Groups[2].Value, color)
                        : match.Value);
                if (replaced == result)
                {
                    break;
                }

                result = replaced;
            }

            return result;
        }

        private static string WrapColor(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
    }
}
