using System;
using UnityEngine;

namespace App.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetPathAttribute : PropertyAttribute
    {
        public Type Type { get; }

        public AssetPathAttribute(Type type)
        {
            Type = type;
        }
    }
}