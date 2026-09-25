using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 強化/弱化効果の文字色の設定（EffectTextStyler が使う）。
    /// アップグレード説明の効果値（ParameterType が Positive/Negative）と、文言中の &lt;p&gt;/&lt;n&gt; タグをこの色で表示する
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeDescriptionStyle", menuName = "Config/UpgradeDescriptionStyle")]
    public class UpgradeDescriptionStyle : ScriptableObject
    {
        [SerializeField, Tooltip("強化効果（ParameterType.Positive）の値の文字色")]
        private Color _positiveColor = new(0.25f, 0.55f, 1f);

        [SerializeField, Tooltip("弱化効果（ParameterType.Negative）の値の文字色")]
        private Color _negativeColor = new(1f, 0.3f, 0.3f);

        public Color PositiveColor => _positiveColor;
        public Color NegativeColor => _negativeColor;
    }
}
