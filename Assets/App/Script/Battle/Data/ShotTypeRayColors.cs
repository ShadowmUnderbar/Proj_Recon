using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    public abstract class ShotTypeRayColors
    {
        private static readonly Color NormalColor = Color.red;
        private static readonly Color MergeColor = new(1,1,0);
        private static readonly Color WaltzColor = new(1,0.5f,0);

        public static Color GetRayColor(ShotType shotType)
        {
            return shotType switch
            {
                ShotType.Normal => NormalColor,
                ShotType.Merge => MergeColor,
                ShotType.Waltz => WaltzColor,
                _ => NormalColor,
            };
        }
    }
}