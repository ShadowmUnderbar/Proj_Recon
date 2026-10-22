using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Views.Afterimage
{
    /// <summary>
    /// 残像1体ぶんの形状と姿勢。体と頭を別々のメッシュのまま、生成時点のワールド行列と一緒に持つ。
    /// SkinnedMeshRenderer はその時点のポーズを静的メッシュへ焼き、MeshRenderer は共有メッシュをそのまま参照する。
    /// 焼いたメッシュはこのインスタンスが持ち続け、次に使い回すときも作り直さずに上書きする。
    /// </summary>
    public sealed class AfterimageSnapshot : IDisposable
    {
        /// <summary>描画1回ぶん（メッシュのサブメッシュ1つ）</summary>
        public readonly struct Part
        {
            public readonly Mesh Mesh;
            public readonly int SubMeshIndex;
            public readonly Matrix4x4 LocalToWorld;
            public readonly Texture BaseMap;

            public Part(Mesh mesh, int subMeshIndex, Matrix4x4 localToWorld, Texture baseMap)
            {
                Mesh = mesh;
                SubMeshIndex = subMeshIndex;
                LocalToWorld = localToWorld;
                BaseMap = baseMap;
            }
        }

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private readonly List<Part> _parts = new();
        private readonly List<Mesh> _bakedMeshes = new();
        private readonly List<Material> _materialBuffer = new();

        public IReadOnlyList<Part> Parts => _parts;

        /// <summary>残像全体の基準点（体の位置）。欠損模様と水平線カーブの基準に使う</summary>
        public Vector3 Pivot { get; private set; }

        /// <summary>カリング用のワールドバウンズ（カーブで沈む分はまだ含まない）</summary>
        public Bounds WorldBounds { get; private set; }

        /// <summary>体の renderingLayerMask。セピア調のグループを元のプレイヤーと揃える</summary>
        public uint RenderingLayerMask { get; private set; }

        public bool IsEmpty => _parts.Count == 0;

        /// <summary>
        /// <paramref name="renderers"/> の今の形状と姿勢を写し取る。1つ目を体（基準点）として扱う。
        /// 以前の内容は捨てるが、焼き込み用のメッシュは使い回す
        /// </summary>
        public void Capture(IReadOnlyList<Renderer> renderers)
        {
            _parts.Clear();
            if (renderers.Count == 0)
            {
                return;
            }

            var body = renderers[0];
            Pivot = body.transform.position;
            RenderingLayerMask = body.renderingLayerMask;

            var bakedCount = 0;
            var bounds = new Bounds();
            var hasBounds = false;

            foreach (var source in renderers)
            {
                Mesh mesh;
                Matrix4x4 localToWorld;

                if (source is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null)
                    {
                        continue;
                    }

                    mesh = GetBakeTarget(bakedCount++);
                    // useScale: true で、親階層の拡縮（非一様を含む）はボーン行列ごと頂点へ焼き込まれる。
                    // 焼いた結果は Renderer の位置・回転だけを基準にした空間になるため、行列はスケール1で組む
                    skinned.BakeMesh(mesh, true);
                    var t = skinned.transform;
                    localToWorld = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                }
                else
                {
                    // MeshRenderer は形が変わらないので共有メッシュを参照する。
                    // 行列は localToWorldMatrix をそのまま写すので、親の非一様スケールによるせん断も再現される
                    if (!source.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    mesh = filter.sharedMesh;
                    localToWorld = source.localToWorldMatrix;
                }

                source.GetSharedMaterials(_materialBuffer);
                var subMeshCount = mesh.subMeshCount;
                for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                {
                    var material = _materialBuffer.Count == 0
                        ? null
                        : _materialBuffer[Mathf.Min(subMeshIndex, _materialBuffer.Count - 1)];
                    _parts.Add(new Part(mesh, subMeshIndex, localToWorld, GetBaseMap(material)));
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(source.bounds);
                }
                else
                {
                    bounds = source.bounds;
                    hasBounds = true;
                }
            }

            WorldBounds = bounds;
        }

        /// <summary>中身を空にする（焼き込み用のメッシュは残す）</summary>
        public void Clear()
        {
            _parts.Clear();
        }

        public void Dispose()
        {
            _parts.Clear();
            foreach (var mesh in _bakedMeshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }

            _bakedMeshes.Clear();
        }

        private Mesh GetBakeTarget(int index)
        {
            if (index < _bakedMeshes.Count)
            {
                return _bakedMeshes[index];
            }

            var mesh = new Mesh
            {
                name = "AfterimageBakedMesh",
                // 生成メッシュをシーンへ保存させない
                hideFlags = HideFlags.DontSave,
            };
            mesh.MarkDynamic();
            _bakedMeshes.Add(mesh);
            return mesh;
        }

        /// <summary>元マテリアルの基本テクスチャ。陰影に明暗だけを薄く混ぜるのに使う</summary>
        private static Texture GetBaseMap(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty(BaseMapId))
            {
                return material.GetTexture(BaseMapId);
            }

            return material.HasProperty(MainTexId) ? material.GetTexture(MainTexId) : null;
        }
    }
}
