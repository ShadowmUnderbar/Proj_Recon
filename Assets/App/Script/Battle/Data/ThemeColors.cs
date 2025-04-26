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
        public static Color LongFocus { get; } = new(0.3f, 0.6f, 1);
        public static Color Dodge { get; } = new(0.2f, 0.8f, 0.2f);
        public static Color Merge { get; } = new(0.2f, 0.2f, 0.3f);
        public static Color TraceLine { get; } = new(1, 1, 0);
        public static Color Conditional { get; } = new(0.9f, 0.8f, 0.9f);

        public static Color GetRayColor(ShotType shotType)
        {
            return shotType switch
            {
                ShotType.Normal => Normal,
                ShotType.Merge => Merge,
                ShotType.Waltz => Waltz,
                _ => Normal,
            };
        }

        public static Color AddFocusColor(Color baseColor)
        {
            return (baseColor + Focus) * 0.5f;
        }

        public static Color AddLongFocusColor(Color baseColor)
        {
            return (baseColor + LongFocus) * 0.5f;
        }
    }
}