// 以下のリポジトリを参考
// https://github.com/ByronMayne/AssetPathAttribute

using System.Collections.Generic;
using App.Framework.Attributes;
using App.Script.Framework.Attributes;
using UnityEditor;
using UnityEngine;

namespace App.Framework.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(AssetPathAttribute))]
    public class AssetPathAttributeDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, Object> _assetReferenceCache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                ShowInvalidAttributeMessage(position, label,
                    $"{nameof(AssetPathAttribute)} invalid for type {property.propertyType}");
                return;
            }

            var assetPath = property.stringValue;
            var assetType = (attribute as AssetPathAttribute)?.Type;

            var asset = TryLoadAsset(assetPath, assetType, property.propertyPath);

            // アセットのロードに失敗した場合は、エラーメッセージを表示し、パスの修正を促す
            if (asset == null && !string.IsNullOrWhiteSpace(property.stringValue))
            {
                EditorGUI.PropertyField(position, property, label);
                EditorGUILayout.HelpBox($"Invalid asset path. Please verify the asset path.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();

            var newAsset = EditorGUI.ObjectField(position, label, asset, assetType, false);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            string newAssetPath = null;

            if (newAsset != null)
            {
                newAssetPath = AssetDatabase.GetAssetPath(newAsset);
            }

            _assetReferenceCache[property.propertyPath] = newAsset;
            property.stringValue = newAssetPath;
        }

        private Object TryLoadAsset(string assetPath, System.Type assetType, string propertyPath)
        {
            Object asset = null;

            if (_assetReferenceCache.TryGetValue(propertyPath, out var value))
            {
                asset = value;
            }

            if (asset != null || string.IsNullOrWhiteSpace(assetPath))
            {
                return asset;
            }

            asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            _assetReferenceCache[propertyPath] = asset;

            return asset;
        }

        private static void ShowInvalidAttributeMessage(Rect position, GUIContent label, string message)
        {
            var labelPosition = position;
            labelPosition.width = EditorGUIUtility.labelWidth;

            GUI.Label(labelPosition, label);

            var contentPosition = position;
            contentPosition.x += EditorGUIUtility.labelWidth;
            contentPosition.width -= EditorGUIUtility.labelWidth;

            EditorGUI.HelpBox(contentPosition, message, MessageType.Error);
        }
    }
}