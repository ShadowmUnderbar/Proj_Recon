using System;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 残像1体ぶんの見た目。生成時に渡し、寿命の間は <see cref="AfterimageConfig"/> の曲線で不透明度・欠損量だけを動かす。
    /// 透過は使わず、不透明度（全体のフェード）も欠けるブロックの量で表す。不透明度は欠損量で残る部分にさらに掛ける。
    /// </summary>
    [Serializable]
    public struct AfterimageStyle
    {
        [Tooltip("残像の色。元テクスチャの色相は使わず、この色を主体に塗る")]
        public Color Color;

        [Range(0f, 1f), Tooltip("彩度。0で灰色、1で指定色そのまま")]
        public float Saturation;

        [Range(0f, 1f), Tooltip("生成直後の不透明度。透過ではなく、残すブロックの割合として効く（1で欠損量どおり、0で全部欠ける）。寿命に応じて曲線で下げる")]
        public float Opacity;

        [Range(0f, 4f), Tooltip("陰影のコントラスト。0で平坦、1で素の陰影、大きいほど明暗がはっきりする")]
        public float ShadeContrast;

        [Range(0f, 1f), Tooltip("生成直後の欠損量（表面が欠ける割合）。寿命に応じて曲線で1へ近づける")]
        public float Dissolve;

        [Min(0.01f), Tooltip("欠損模様の細かさ（1mあたりのブロック数）")]
        public float NoiseScale;

        [Min(0f), Tooltip("欠損模様が組み替わる速さ（回/秒）。0で模様を固定する")]
        public float NoiseSpeed;

        [ColorUsage(false, true), Tooltip("欠損の切り口を縁取る色（HDR）")]
        public Color EdgeColor;

        [Range(0f, 0.5f), Tooltip("切り口の縁取りの幅（欠損のしきい値からの差）。0で縁取りなし")]
        public float EdgeWidth;

        public static AfterimageStyle Default => new()
        {
            Color = new Color(0.3f, 0.85f, 1f, 1f),
            Saturation = 0.8f,
            Opacity = 0.7f,
            ShadeContrast = 1.2f,
            Dissolve = 0.15f,
            NoiseScale = 12f,
            NoiseSpeed = 8f,
            EdgeColor = new Color(0.6f, 1.6f, 2f, 1f),
            EdgeWidth = 0.08f,
        };
    }
}
