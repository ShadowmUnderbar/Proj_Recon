using UnityEngine;
using UnityEngine.Rendering;

namespace App.Battle.Views.Afterimage
{
    /// <summary>
    /// 残像を描く。GameObject や Renderer は作らず、写し取ったメッシュと行列を Graphics.RenderMesh で直接描く。
    /// マテリアルは共有のまま、インスタンスごとの値は MaterialPropertyBlock で渡す（マテリアルを複製しない）。
    /// </summary>
    public sealed class AfterimageDrawer
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int ShadeContrastId = Shader.PropertyToID("_ShadeContrast");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int PivotId = Shader.PropertyToID("_AfterimagePivot");
        private static readonly int SeedId = Shader.PropertyToID("_AfterimageSeed");
        private static readonly int AgeId = Shader.PropertyToID("_AfterimageAge");

        private readonly Material _material;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private readonly int _layer;
        private readonly float _cullingDropMargin;

        /// <param name="material">App/Afterimage シェーダのマテリアル（共有）</param>
        /// <param name="layer">描画するレイヤー（カメラのカリングマスクに含まれるもの）</param>
        /// <param name="cullingDropMargin">水平線カーブで沈む分だけ、カリング用バウンズを下へ広げる量[m]</param>
        public AfterimageDrawer(Material material, int layer, float cullingDropMargin)
        {
            _material = material;
            _layer = layer;
            _cullingDropMargin = cullingDropMargin;
        }

        /// <summary>
        /// 1体ぶんを描く。不透明度と欠損量は寿命に応じた値を渡す（生成時の見た目からの変化は呼び出し側が決める）
        /// </summary>
        public void Draw(AfterimageInstance instance, float opacity, float dissolve)
        {
            if (_material == null || opacity <= 0f || dissolve >= 1f)
            {
                return;
            }

            var snapshot = instance.Snapshot;
            var style = instance.Style;

            _propertyBlock.Clear();
            _propertyBlock.SetColor(ColorId, style.Color);
            _propertyBlock.SetFloat(SaturationId, style.Saturation);
            _propertyBlock.SetFloat(OpacityId, opacity);
            _propertyBlock.SetFloat(ShadeContrastId, style.ShadeContrast);
            _propertyBlock.SetFloat(DissolveId, dissolve);
            _propertyBlock.SetFloat(NoiseScaleId, style.NoiseScale);
            _propertyBlock.SetFloat(NoiseSpeedId, style.NoiseSpeed);
            _propertyBlock.SetColor(EdgeColorId, style.EdgeColor);
            _propertyBlock.SetFloat(EdgeWidthId, style.EdgeWidth);
            _propertyBlock.SetVector(PivotId, snapshot.Pivot);
            _propertyBlock.SetFloat(SeedId, instance.Seed);
            _propertyBlock.SetFloat(AgeId, instance.Age);

            // カーブは基準点の真下へ沈めるので、バウンズも下へ広げる（広げないと沈んだ残像が先に消える）
            var bounds = snapshot.WorldBounds;
            bounds.Encapsulate(new Bounds(bounds.center + Vector3.down * _cullingDropMargin, bounds.size));

            var renderParams = new RenderParams(_material)
            {
                layer = _layer,
                renderingLayerMask = snapshot.RenderingLayerMask,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                worldBounds = bounds,
                matProps = _propertyBlock,
            };

            var parts = snapshot.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                // 共有メッシュは元の頭と一緒に破棄されうる（実行時に作られたメッシュの場合）。その部分だけ描かない
                if (part.Mesh == null)
                {
                    continue;
                }

                // パーツごとに違うのは元テクスチャだけ。RenderMesh は呼んだ時点のブロックを写すので上書きしてよい
                _propertyBlock.SetTexture(BaseMapId, part.BaseMap != null ? part.BaseMap : Texture2D.whiteTexture);
                Graphics.RenderMesh(renderParams, part.Mesh, part.SubMeshIndex, part.LocalToWorld);
            }
        }
    }
}
