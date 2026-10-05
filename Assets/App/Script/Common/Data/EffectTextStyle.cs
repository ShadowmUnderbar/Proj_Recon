using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// 文言中の独自タグ（&lt;p&gt;…&lt;/p&gt; など）を、どの色で表示するかの対応表（EffectTextStyler が使う）。
    /// アップグレード説明・チュートリアル本文のタグと、アップグレード説明の効果値
    /// （ParameterType.Positive は "p"、Negative は "n" のタグの色）をこの色で表示する
    /// </summary>
    [CreateAssetMenu(fileName = "EffectTextStyle", menuName = "Config/EffectTextStyle")]
    public class EffectTextStyle : ScriptableObject
    {
        /// <summary>タグ名1件と、その中身を表示する色</summary>
        [Serializable]
        public class ColorTag
        {
            [SerializeField, Tooltip("タグ名（英数字のみ）。\"p\" なら <p>…</p> を置き換える。" +
                                     "TextMeshPro の標準タグ（b, i, u, s, color, size など）と同じ名前は使わない")]
            private string _tagName;

            [SerializeField, Tooltip("タグの中身の文字色")]
            private Color _color = Color.white;

            [SerializeField, Tooltip("用途のメモ（表示には使わない）")]
            private string _memo;

            public string TagName => _tagName;
            public Color Color => _color;

            public ColorTag(string tagName, Color color, string memo)
            {
                _tagName = tagName;
                _color = color;
                _memo = memo;
            }
        }

        // 初期値: p/n は強化・弱化色、w/m/f は ShotLineColorConfig のライン色と同じ値（f はノーマル＋フォーカス時）、o はセピア色
        [SerializeField, Tooltip("タグ名と色の対応。同じタグ名が複数あると先のものを使う")]
        private List<ColorTag> _colorTags = new()
        {
            new ColorTag("p", new Color(0.25f, 0.55f, 1f), "強化効果（ParameterType.Positive の効果値も含む）"),
            new ColorTag("n", new Color(1f, 0.3f, 0.3f), "弱化効果（ParameterType.Negative の効果値も含む）"),
            new ColorTag("w", new Color(1f, 0.5f, 0f), "ワルツショット（ラインと同じ色）"),
            new ColorTag("m", new Color(0.2f, 0.2f, 0.3f), "マージショット（ラインと同じ色）"),
            new ColorTag("f", new Color(1f, 0.3f, 1f), "フォーカスショット（ノーマル＋フォーカス時のラインと同じ色）"),
            new ColorTag("o", new Color(0.85f, 0.66f, 0.42f), "オーバークロック（セピア色）"),
        };

        // タグ名に使える文字（正規表現に埋め込むため英数字に限る）
        private static readonly Regex ValidTagName = new("^[A-Za-z0-9]+$", RegexOptions.Compiled);

        // タグ名→色の引き表と、登録済みタグをまとめて探す正規表現。初回利用時に作り、Inspector で変更されたら作り直す
        private Dictionary<string, Color> _colorByTag;
        private Regex _tagPattern;

        /// <summary>タグ名に対応する色を引く。未登録なら false</summary>
        public bool TryGetColor(string tagName, out Color color)
        {
            EnsureCache();
            return _colorByTag.TryGetValue(tagName, out color);
        }

        /// <summary>
        /// 登録済みタグのどれかに一致する正規表現（グループ1=タグ名、グループ2=中身）。
        /// 有効なタグが1件もなければ null
        /// </summary>
        public Regex TagPattern
        {
            get
            {
                EnsureCache();
                return _tagPattern;
            }
        }

        private void EnsureCache()
        {
            if (_colorByTag != null)
            {
                return;
            }

            _colorByTag = new Dictionary<string, Color>();
            foreach (var colorTag in _colorTags)
            {
                if (colorTag == null || !IsValidTagName(colorTag.TagName))
                {
                    continue;
                }

                _colorByTag.TryAdd(colorTag.TagName, colorTag.Color);
            }

            // 開き・閉じが対になったものだけを拾う（閉じ忘れは置換せず文字のまま残し、表示で気づけるようにする）。
            // TextMeshPro の <page> / <pos=…> 等を巻き込まないよう、タグ名は完全一致で探す
            _tagPattern = _colorByTag.Count == 0
                ? null
                : new Regex($"<({string.Join("|", _colorByTag.Keys)})>(.*?)</\\1>", RegexOptions.Singleline);
        }

        private static bool IsValidTagName(string tagName)
        {
            return !string.IsNullOrEmpty(tagName) && ValidTagName.IsMatch(tagName);
        }

        private void OnEnable()
        {
            _colorByTag = null;
        }

        private void OnValidate()
        {
            _colorByTag = null;

            var seen = new HashSet<string>();
            foreach (var colorTag in _colorTags)
            {
                if (colorTag == null)
                {
                    continue;
                }

                if (!IsValidTagName(colorTag.TagName))
                {
                    Debug.LogWarning($"[EffectTextStyle] タグ名 '{colorTag.TagName}' は使えません（英数字のみ）。この行は無視します", this);
                }
                else if (!seen.Add(colorTag.TagName))
                {
                    Debug.LogWarning($"[EffectTextStyle] タグ名 '{colorTag.TagName}' が重複しています。先の行の色を使います", this);
                }
            }
        }
    }
}
