using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// アップグレード詳細説明の効果値の装飾設定。
    /// 値の ParameterType が強化（Positive）/弱化（Negative）のとき、埋め込んだ数値をこの色の太字で表示する
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
