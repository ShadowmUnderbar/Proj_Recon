using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    public abstract class ThemeColors
    {
        public static Color None { get; } = new(0.5f, 0.5f, 0.5f);
        public static Color Normal { get; } = Color.red;
        public static Color Focus { get; } = new(1f, 0.3f, 1f);
        public static Color Waltz { get; } = new(1, 0.5f, 0);
        public static Color Dodge { get; } = new(0.2f, 0.8f, 0.2f);
        public static Color Merge { get; } = new(0.2f, 0.2f, 0.3f);
        public static Color TraceLine { get; } = new(1, 1, 0);
        public static Color Conditional { get; } = new(0.9f, 0.8f, 0.9f);

        public static Color GetRayColor(ShotType shotType, AimFocusType focusType)
        {
            if (shotType == ShotType.Normal)
            {
                return focusType switch
                {
                    AimFocusType.NotFocus => Normal,
                    AimFocusType.Focus => Focus,
                    _ => Normal,
                };
            }

            var color = shotType switch
            {
                ShotType.Merge => Merge,
                ShotType.Waltz => Waltz,
                _ => Normal,
            };

            return focusType switch
            {
                AimFocusType.NotFocus => color,
                AimFocusType.Focus => AddFocusColor(color),
                _ => color,
            };
        }

        public static Color AddFocusColor(Color baseColor)
        {
            return (baseColor + Focus) * 0.5f;
        }
    }
}