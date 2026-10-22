using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

namespace App.Battle.Views.FloorCollapse
{
    /// <summary>
    /// 床崩壊の見た目。崩壊中だけ、目印（<see cref="FloorCollapseTargetView"/>）の付いた床を隠して演出用のプレート床へ差し替える。
    /// プレートの沈下・傾斜・スライド・データ片化はすべてシェーダ（App/FloorCollapse）が進行度から決めるので、
    /// ここが毎フレーム行うのは進行度を1つ渡すことだけ。GameObject・物理・プレートごとの Transform は使わない。
    /// プレートのメッシュは分割密度・広さ・厚みが変わったときだけ作り直す
    /// </summary>
    public class FloorCollapseView : MonoBehaviour, IFloorCollapseView
    {
        [SerializeField, Tooltip("水平線カーブで沈む分のカリング余裕を読む設定")]
        private CurvedWorldConfig _curvedWorldConfig;

        private static readonly int ProgressId = Shader.PropertyToID("_CollapseProgress");
        private static readonly int OriginId = Shader.PropertyToID("_CollapseOrigin");
        private static readonly int DirectionId = Shader.PropertyToID("_CollapseDirection");
        private static readonly int MaxTravelId = Shader.PropertyToID("_MaxTravel");
        private static readonly int StaggerId = Shader.PropertyToID("_Stagger");
        private static readonly int JitterId = Shader.PropertyToID("_Jitter");
        private static readonly int CrackPortionId = Shader.PropertyToID("_CrackPortion");
        private static readonly int SinkDepthId = Shader.PropertyToID("_SinkDepth");
        private static readonly int TiltAngleId = Shader.PropertyToID("_TiltAngle");
        private static readonly int SlideDistanceId = Shader.PropertyToID("_SlideDistance");
        private static readonly int GapRatioId = Shader.PropertyToID("_GapRatio");
        private static readonly int DissolveStartId = Shader.PropertyToID("_DissolveStart");
        private static readonly int FragmentsPerPlateId = Shader.PropertyToID("_FragmentsPerPlate");
        private static readonly int BlackoutStartId = Shader.PropertyToID("_BlackoutStart");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
        private static readonly int HalfPlateSizeId = Shader.PropertyToID("_HalfPlateSize");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int PlanarRectId = Shader.PropertyToID("_PlanarRect");

        private FloorCollapseConfig _config;

        private MeshRenderer _plateRenderer;
        private MeshRenderer _voidRenderer;
        private Mesh _plateMesh;
        private Mesh _voidMesh;
        private MaterialPropertyBlock _propertyBlock;

        // 作り直しの判定に使う、今のメッシュの組み立て条件
        private float _builtPlateSize = -1f;
        private float _builtFieldSize = -1f;
        private float _builtThickness = -1f;

        private readonly List<Renderer> _hiddenTargets = new();

        public int PlateCount { get; private set; }

        [Inject]
        public void Construct(FloorCollapseConfig config)
        {
            _config = config;
            _propertyBlock = new MaterialPropertyBlock();
            _plateRenderer = CreateRendererObject("Plates", config.PlateMaterial, out _plateMesh);
            _voidRenderer = CreateRendererObject("Void", config.VoidMaterial, out _voidMesh);
            BuildVoidMesh(_voidMesh);

            if (config.PlateMaterial == null)
            {
                Debug.LogWarning("[FloorCollapseView] FloorCollapseConfig にプレートのマテリアルが設定されていないため、床崩壊は描かれません");
            }

            SetVisible(false);
        }

        public void Begin(Vector3 origin, FloorCollapseSettings settings)
        {
            if (_config == null)
            {
                return;
            }

            // やり直しのときは、前回隠した床を戻してから隠し直す
            RestoreTargets();

            RebuildPlatesIfNeeded(settings);

            // 床の見た目・高さは最初に見つかった対象から取る。プレートの境目が毎回同じワールド位置に来るよう、中心をプレートの格子へ揃える
            var floorHeight = origin.y;
            var renderingLayerMask = _plateRenderer.renderingLayerMask;
            _propertyBlock.Clear();

            var targets = FindObjectsByType<FloorCollapseTargetView>(FindObjectsSortMode.None);
            var hasLook = false;
            foreach (var target in targets)
            {
                var targetRenderer = target.TargetRenderer;
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                if (!hasLook)
                {
                    hasLook = true;
                    floorHeight = targetRenderer.bounds.max.y;
                    renderingLayerMask = targetRenderer.renderingLayerMask;
                    CopyFloorLook(targetRenderer);
                }

                targetRenderer.enabled = false;
                _hiddenTargets.Add(targetRenderer);
            }

            var plateSize = settings.PlateSize;
            var center = new Vector3(
                Mathf.Round(origin.x / plateSize) * plateSize,
                floorHeight,
                Mathf.Round(origin.z / plateSize) * plateSize);
            transform.SetPositionAndRotation(center, Quaternion.identity);
            _voidRenderer.transform.localPosition = Vector3.down * _config.VoidDepth;

            var direction = settings.NormalizedDirection;
            _propertyBlock.SetVector(OriginId, new Vector4(origin.x - center.x, 0f, origin.z - center.z, 0f));
            _propertyBlock.SetVector(DirectionId, new Vector4(direction.x, direction.y, (float)settings.Mode, 0f));
            _propertyBlock.SetFloat(MaxTravelId, settings.MaxTravelDistance);
            _propertyBlock.SetFloat(StaggerId, settings.Stagger);
            _propertyBlock.SetFloat(JitterId, settings.Jitter);
            _propertyBlock.SetFloat(CrackPortionId, settings.CrackPortion);
            _propertyBlock.SetFloat(SinkDepthId, settings.SinkDepth);
            _propertyBlock.SetFloat(TiltAngleId, settings.TiltAngle * Mathf.Deg2Rad);
            _propertyBlock.SetFloat(SlideDistanceId, settings.SlideDistance);
            _propertyBlock.SetFloat(GapRatioId, settings.GapRatio);
            _propertyBlock.SetFloat(DissolveStartId, settings.DissolveStart);
            _propertyBlock.SetFloat(FragmentsPerPlateId, settings.FragmentsPerPlate);
            _propertyBlock.SetFloat(BlackoutStartId, settings.BlackoutStart);
            _propertyBlock.SetColor(GlowColorId, settings.GlowColor);
            _propertyBlock.SetFloat(LineWidthId, settings.LineWidth);
            _propertyBlock.SetFloat(HalfPlateSizeId, plateSize * 0.5f);
            _propertyBlock.SetFloat(ProgressId, 0f);

            // セピア調のグループは元の床に揃える
            _plateRenderer.renderingLayerMask = renderingLayerMask;
            _voidRenderer.renderingLayerMask = renderingLayerMask;
            _plateRenderer.SetPropertyBlock(_propertyBlock);
            SetVisible(true);
        }

        public void SetProgress(float progress)
        {
            if (_propertyBlock == null || !_plateRenderer.enabled)
            {
                return;
            }

            _propertyBlock.SetFloat(ProgressId, progress);
            _plateRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void Restore()
        {
            if (_plateRenderer == null)
            {
                return;
            }

            SetVisible(false);
            RestoreTargets();
        }

        private void RebuildPlatesIfNeeded(FloorCollapseSettings settings)
        {
            if (Mathf.Approximately(_builtPlateSize, settings.PlateSize)
                && Mathf.Approximately(_builtFieldSize, settings.FieldSize)
                && Mathf.Approximately(_builtThickness, settings.PlateThickness))
            {
                UpdatePlateBounds(settings);
                return;
            }

            PlateCount = FloorCollapsePlateMeshBuilder.Build(_plateMesh, settings.PlateSize, settings.FieldSize, settings.PlateThickness);
            _builtPlateSize = settings.PlateSize;
            _builtFieldSize = settings.FieldSize;
            _builtThickness = settings.PlateThickness;
            UpdatePlateBounds(settings);
        }

        /// <summary>
        /// 沈下量・スライド量は作り直しの条件に入れていないので、バウンズだけは再生のたびに取り直す
        /// </summary>
        private void UpdatePlateBounds(FloorCollapseSettings settings)
        {
            // 頂点はシェーダで沈む・傾く・ずれるので、CPU側のバウンズを動きの最大範囲まで広げておく
            // （水平線カーブの沈下分も含める。広げないと崩れかけのプレートがカリングで消える）
            var curveMargin = _curvedWorldConfig != null ? _curvedWorldConfig.CullingDropMargin : 0f;
            var reach = settings.FieldSize * 0.5f + settings.SlideDistance + settings.PlateSize;
            var depth = settings.SinkDepth + settings.PlateSize + settings.PlateThickness + curveMargin;
            _plateMesh.bounds = new Bounds(
                new Vector3(0f, -depth * 0.5f, 0f),
                new Vector3(reach * 2f, depth + settings.PlateSize, reach * 2f));
        }

        /// <summary>元の床の基本テクスチャと色を、真上からの投影で写すための値として渡す（元のUVは使わない）</summary>
        private void CopyFloorLook(Renderer targetRenderer)
        {
            var bounds = targetRenderer.bounds;
            _propertyBlock.SetVector(PlanarRectId, new Vector4(bounds.min.x, bounds.min.z,
                Mathf.Max(bounds.size.x, Mathf.Epsilon), Mathf.Max(bounds.size.z, Mathf.Epsilon)));

            var material = targetRenderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseMapId) && material.GetTexture(BaseMapId) != null)
            {
                _propertyBlock.SetTexture(BaseMapId, material.GetTexture(BaseMapId));
                _propertyBlock.SetVector(BaseMapStId, material.GetVector(BaseMapStId));
            }
            else if (material.HasProperty(MainTexId) && material.GetTexture(MainTexId) != null)
            {
                _propertyBlock.SetTexture(BaseMapId, material.GetTexture(MainTexId));
                _propertyBlock.SetVector(BaseMapStId, material.GetVector(MainTexStId));
            }

            if (material.HasProperty(BaseColorId))
            {
                _propertyBlock.SetColor(BaseColorId, material.GetColor(BaseColorId));
            }
            else if (material.HasProperty(ColorId))
            {
                _propertyBlock.SetColor(BaseColorId, material.GetColor(ColorId));
            }
        }

        private void RestoreTargets()
        {
            foreach (var target in _hiddenTargets)
            {
                if (target != null)
                {
                    target.enabled = true;
                }
            }

            _hiddenTargets.Clear();
        }

        private void SetVisible(bool visible)
        {
            _plateRenderer.enabled = visible;
            _voidRenderer.enabled = visible;
        }

        private MeshRenderer CreateRendererObject(string objectName, Material material, out Mesh mesh)
        {
            var child = new GameObject(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            mesh = new Mesh
            {
                name = $"FloorCollapse{objectName}",
                // 生成メッシュをシーンへ保存させない
                hideFlags = HideFlags.DontSave,
            };
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return meshRenderer;
        }

        /// <summary>
        /// プレートの隙間から見える暗い空間。水平線カーブに沿うよう格子で張る
        /// </summary>
        private static void BuildVoidMesh(Mesh mesh)
        {
            const float size = 400f;
            const int divisions = 40;
            var verticesPerSide = divisions + 1;
            var vertices = new Vector3[verticesPerSide * verticesPerSide];
            var normals = new Vector3[vertices.Length];
            var step = size / divisions;
            for (var z = 0; z < verticesPerSide; z++)
            {
                for (var x = 0; x < verticesPerSide; x++)
                {
                    var index = z * verticesPerSide + x;
                    vertices[index] = new Vector3(x * step - size * 0.5f, 0f, z * step - size * 0.5f);
                    normals[index] = Vector3.up;
                }
            }

            var triangles = new int[divisions * divisions * 6];
            var t = 0;
            for (var z = 0; z < divisions; z++)
            {
                for (var x = 0; x < divisions; x++)
                {
                    var bottomLeft = z * verticesPerSide + x;
                    var topLeft = bottomLeft + verticesPerSide;
                    triangles[t++] = bottomLeft;
                    triangles[t++] = topLeft;
                    triangles[t++] = bottomLeft + 1;
                    triangles[t++] = bottomLeft + 1;
                    triangles[t++] = topLeft;
                    triangles[t++] = topLeft + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            RestoreTargets();
            if (_plateMesh != null)
            {
                Destroy(_plateMesh);
            }

            if (_voidMesh != null)
            {
                Destroy(_voidMesh);
            }
        }
    }
}
