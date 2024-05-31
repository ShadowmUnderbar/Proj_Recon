using UnityEngine;

namespace App.Script.Framework.Utilities.Extensions
{
    public static class TopdownVector2Extensions
    {
        public static Vector2 ToTopdown(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        } 
    }
}