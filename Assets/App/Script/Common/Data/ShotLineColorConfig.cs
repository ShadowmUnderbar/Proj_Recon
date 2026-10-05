using UnityEngine;

namespace App.Common.Data
{
    /// <summary>ShotLineColorConfig から引けるライン色の種類（文字色の連動先に使う）</summary>
    public enum ShotLineColorType
    {
        /// <summary>ノーマルショット（非フォーカス）</summary>
        Normal,

        /// <summary>フォーカスショット（ノーマル＋フォーカス）</summary>
        Focus,

        /// <summary>ワルツショット（非フォーカス）</summary>
        Waltz,

        /// <summary>マージショット（非フォーカス）</summary>
        Merge,
    }

    /// <summary>
    /// 射撃形態ごとの照準ライン（手元・エイムのレイ）の色。
    /// PlayerShotUseCase がレイの色に、EffectTextStyle が文言の色タグ（&lt;w&gt; など）の色に使うので、ここを変えると両方が変わる
    /// </summary>
    [CreateAssetMenu(fileName = "ShotLineColorConfig", menuName = "Config/ShotLineColorConfig")]
    public class ShotLineColorConfig : ScriptableObject
    {
        [SerializeField, Tooltip("ノーマルショット（非フォーカス）のライン色")]
        private Color _normalColor = Color.red;

        [SerializeField, Tooltip("フォーカス時のライン色。ノーマルはこの色そのもの、ワルツ・マージは各色とこの色の平均になる")]
        private Color _focusColor = new(1f, 0.3f, 1f);

        [SerializeField, Tooltip("ワルツショット（非フォーカス）のライン色")]
        private Color _waltzColor = new(1f, 0.5f, 0f);

        [SerializeField, Tooltip("マージショット（非フォーカス）のライン色")]
        private Color _mergeColor = new(0.2f, 0.2f, 0.3f);

        public Color GetColor(ShotLineColorType type)
        {
            return type switch
            {
                ShotLineColorType.Focus => _focusColor,
                ShotLineColorType.Waltz => _waltzColor,
                ShotLineColorType.Merge => _mergeColor,
                _ => _normalColor,
            };
        }

        /// <summary>射撃形態とフォーカス状態からレイの色を決める</summary>
        public Color GetRayColor(ShotType shotType, AimFocusType focusType)
        {
            var isFocus = focusType == AimFocusType.Focus;

            if (shotType == ShotType.Normal)
            {
                return isFocus ? _focusColor : _normalColor;
            }

            var color = shotType switch
            {
                ShotType.Merge => _mergeColor,
                ShotType.Waltz => _waltzColor,
                _ => _normalColor,
            };

            // ワルツ・マージのフォーカス時は、形態の色とフォーカス色の中間にする
            return isFocus ? (color + _focusColor) * 0.5f : color;
        }
    }
}
