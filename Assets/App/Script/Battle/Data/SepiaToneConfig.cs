using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// セピア調の見た目の設定。どの演出でも共通の色味をここで持つ。
    /// どのグループにどれだけかけるかは、演出ごとの <see cref="SepiaTonePreset"/> が持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "SepiaToneConfig", menuName = "Config/SepiaToneConfig")]
    public class SepiaToneConfig : ScriptableObject
    {
        [SerializeField, Tooltip("輝度に掛けてセピアの色にする係数。白が (1,1,1) のとき、この色になる。1を超えると元より明るくなる")]
        private Color _toneColor = new(1.12f, 0.94f, 0.7f, 1f);

        public Color ToneColor => _toneColor;
    }
}
