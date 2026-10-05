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
        /// <summary>タグの色をどこから取るか</summary>
        public enum ColorSource
        {
            /// <summary>この行の色（_color）をそのまま使う</summary>
            Custom,

            /// <summary>ShotLineColorConfig のライン色を使う（ライン色を変えると文字色も変わる）</summary>
            ShotLine,
        }

        /// <summary>タグ名1件と、その中身を表示する色</summary>
        [Serializable]
        public class ColorTag
        {
            [SerializeField, Tooltip("タグ名（英数字のみ）。\"p\" なら <p>…</p> を置き換える。" +
                                     "TextMeshPro の標準タグ（b, i, u, s, color, size など）と同じ名前は使わない")]
            private string _tagName;

            [SerializeField, Tooltip("Custom: 下の色を使う / ShotLine: ShotLineColorConfig のライン色を使う")]
            private ColorSource _source;

            [SerializeField, Tooltip("Custom のときの文字色")]
            private Color _color = Color.white;

            [SerializeField, Tooltip("ShotLine のときに使うライン色の種類")]
            private ShotLineColorType _shotLine;

            [SerializeField, Tooltip("用途のメモ（表示には使わない）")]
            private string _memo;

            public string TagName => _tagName;
            public ColorSource Source => _source;
            public Color Color => _color;
            public ShotLineColorType ShotLine => _shotLine;

            public ColorTag(string tagName, Color color, string memo)
            {
                _tagName = tagName;
                _source = ColorSource.Custom;
                _color = color;
                _memo = memo;
            }

            public ColorTag(string tagName, ShotLineColorType shotLine, string memo)
            {
                _tagName = tagName;
                _source = ColorSource.ShotLine;
                _shotLine = shotLine;
                _memo = memo;
            }
        }

        [SerializeField, Tooltip("ShotLine を選んだタグの色の取得元（照準ラインの色設定）")]
        private ShotLineColorConfig _shotLineColorConfig;

        /// <summary>ShotLine を選んだタグの色の取得元（レイ色と同じアセットを指しているかの確認用）</summary>
        public ShotLineColorConfig ShotLineColorConfig => _shotLineColorConfig;

        // 初期値: p/n は強化・弱化色、w/m/f はライン色に連動（f はノーマル＋フォーカス時）、o はセピア色
        [SerializeField, Tooltip("タグ名と色の対応。同じタグ名が複数あると先のものを使う")]
        private List<ColorTag> _colorTags = new()
        {
            new ColorTag("p", new Color(0.25f, 0.55f, 1f), "強化効果（ParameterType.Positive の効果値も含む）"),
            new ColorTag("n", new Color(1f, 0.3f, 0.3f), "弱化効果（ParameterType.Negative の効果値も含む）"),
            new ColorTag("w", ShotLineColorType.Waltz, "ワルツショット（ラインと同じ色）"),
            new ColorTag("m", ShotLineColorType.Merge, "マージショット（ラインと同じ色）"),
            new ColorTag("f", ShotLineColorType.Focus, "フォーカスショット（ノーマル＋フォーカス時のラインと同じ色）"),
            new ColorTag("o", new Color(0.85f, 0.66f, 0.42f), "オーバークロック（セピア色）"),
        };

        // タグ名に使える文字（正規表現に埋め込むため英数字に限る）
        private static readonly Regex ValidTagName = new("^[A-Za-z0-9]+$", RegexOptions.Compiled);

        // タグ名→行の引き表と、登録済みタグをまとめて探す正規表現。初回利用時に作り、Inspector で変更されたら作り直す。
        // 色そのものは引くたびに行・ライン色設定から読む（ライン色設定の変更をそのまま反映するため）
        private Dictionary<string, ColorTag> _tagByName;
        private Regex _tagPattern;

        /// <summary>タグ名に対応する色を引く。未登録、またはライン色設定が未割り当てなら false</summary>
        public bool TryGetColor(string tagName, out Color color)
        {
            EnsureCache();
            if (!_tagByName.TryGetValue(tagName, out var colorTag))
            {
                color = default;
                return false;
            }

            return TryResolveColor(colorTag, out color);
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

        private bool TryResolveColor(ColorTag colorTag, out Color color)
        {
            if (colorTag.Source != ColorSource.ShotLine)
            {
                color = colorTag.Color;
                return true;
            }

            if (_shotLineColorConfig == null)
            {
                color = default;
                return false;
            }

            color = _shotLineColorConfig.GetColor(colorTag.ShotLine);
            return true;
        }

        private void EnsureCache()
        {
            if (_tagByName != null)
            {
                return;
            }

            _tagByName = new Dictionary<string, ColorTag>();
            foreach (var colorTag in _colorTags)
            {
                if (colorTag == null || !IsValidTagName(colorTag.TagName))
                {
                    continue;
                }

                _tagByName.TryAdd(colorTag.TagName, colorTag);
            }

            // 開き・閉じが対になったものだけを拾う（閉じ忘れは置換せず文字のまま残し、表示で気づけるようにする）。
            // TextMeshPro の <page> / <pos=…> 等を巻き込まないよう、タグ名は完全一致で探す
            _tagPattern = _tagByName.Count == 0
                ? null
                : new Regex($"<({string.Join("|", _tagByName.Keys)})>(.*?)</\\1>", RegexOptions.Singleline);
        }

        private static bool IsValidTagName(string tagName)
        {
            return !string.IsNullOrEmpty(tagName) && ValidTagName.IsMatch(tagName);
        }

        private void OnEnable()
        {
            _tagByName = null;
        }

        private void OnValidate()
        {
            _tagByName = null;

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
                else if (colorTag.Source == ColorSource.ShotLine && _shotLineColorConfig == null)
                {
                    Debug.LogWarning($"[EffectTextStyle] タグ '{colorTag.TagName}' はライン色に連動しますが、ShotLineColorConfig が未割り当てです。色を付けずに表示します", this);
                }
            }
        }
    }
}
