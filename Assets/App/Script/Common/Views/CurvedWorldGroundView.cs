using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 水平線カーブ用の地面メッシュを生成する。
    /// 頂点が無い場所は曲がらないため、既定のPlane（一辺10分割）のままでは
    /// ステージ全体が数枚の巨大な三角形になり、カーブがほとんど出ない。
    /// 生成するのは見た目用のメッシュだけで、コライダーには触れない。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CurvedWorldGroundView : MonoBehaviour
    {
        /// <summary>スケールが1から外れているとみなす許容差</summary>
        private const float ScaleTolerance = 0.001f;

        [SerializeField, Tooltip("地面の大きさと分割数を持つ設定")]
        private CurvedWorldConfig _config;

        private static readonly int ParamsId = Shader.PropertyToID("_CurvedWorldParams");

        private Mesh _mesh;
        private MeshFilter _meshFilter;

        /// <summary>バウンズに反映済みの曲率。シェーダへ配られている値と食い違ったら取り直す</summary>
        private float _appliedStrength = -1f;

        private void OnEnable()
        {
            Rebuild();
        }

        private void LateUpdate()
        {
            // 実機で曲率を振ったときに、バウンズだけ追随させる
            var strength = Shader.GetGlobalVector(ParamsId).x;
            if (Mathf.Approximately(strength, _appliedStrength))
            {
                return;
            }

            _appliedStrength = strength;
            UpdateBounds();
        }

        private void OnDestroy()
        {
            if (_mesh == null)
            {
                return;
            }

            // 生成したメッシュはアセットではないので、明示的に破棄しないと漏れる
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }

            _mesh = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 分割数や大きさを触りながら見た目を確かめられるようにする
            // スライダーを動かしている間はOnValidateが連続で飛ぶ。
            // そのたびに登録すると同じ処理が何十回も走ってエディタが固まる
            if (!isActiveAndEnabled || _rebuildQueued)
            {
                return;
            }

            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += RebuildIfAlive;
        }

        private bool _rebuildQueued;

        private void RebuildIfAlive()
        {
            _rebuildQueued = false;

            if (this != null)
            {
                Rebuild();
            }
        }
#endif

        /// <summary>設定にしたがってグリッドメッシュを作り直す</summary>
        public void Rebuild()
        {
            if (_config == null)
            {
                return;
            }

            var scale = transform.lossyScale;
            if (Mathf.Abs(scale.x - 1f) > ScaleTolerance || Mathf.Abs(scale.z - 1f) > ScaleTolerance)
            {
                Debug.LogWarning(
                    $"[CurvedWorldGroundView] スケールが1ではないため、地面の大きさが設定値と一致しない（lossyScale={scale}）",
                    this);
            }

            _meshFilter = _meshFilter != null ? _meshFilter : GetComponent<MeshFilter>();

            if (_mesh == null)
            {
                // DontSaveを付けないと、分割数ぶんの頂点がシーンファイルへ丸ごと焼き込まれる。
                // 一辺120分割で2MBを超えるうえ、分割数を変えても差分が巨大になる
                _mesh = new Mesh
                {
                    name = "CurvedWorldGround",
                    hideFlags = HideFlags.DontSave,
                };
            }

            BuildGrid(_mesh, _config.GroundSize, _config.GroundDivisions);

            // 生成直後の1フレームだけバウンズが0になるのを防ぐ
            _appliedStrength = Shader.GetGlobalVector(ParamsId).x;
            UpdateBounds();
            _meshFilter.sharedMesh = _mesh;
        }

        /// <summary>XZ平面に一辺 size のグリッドを張る</summary>
        private static void BuildGrid(Mesh mesh, float size, int divisions)
        {
            var verticesPerSide = divisions + 1;
            var vertexCount = verticesPerSide * verticesPerSide;

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];

            var step = size / divisions;
            var half = size * 0.5f;

            for (var z = 0; z < verticesPerSide; z++)
            {
                for (var x = 0; x < verticesPerSide; x++)
                {
                    var index = z * verticesPerSide + x;
                    vertices[index] = new Vector3(x * step - half, 0f, z * step - half);
                    normals[index] = Vector3.up;
                    uv[index] = new Vector2((float)x / divisions, (float)z / divisions);
                }
            }

            var triangles = new int[divisions * divisions * 6];
            var triangleIndex = 0;
            for (var z = 0; z < divisions; z++)
            {
                for (var x = 0; x < divisions; x++)
                {
                    var bottomLeft = z * verticesPerSide + x;
                    var topLeft = bottomLeft + verticesPerSide;

                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft + 1;

                    triangles[triangleIndex++] = bottomLeft + 1;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topLeft + 1;
                }
            }

            mesh.Clear();
            // 分割数を上げると頂点が65535を超えるため、32bitインデックスで作る
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

        /// <summary>
        /// 実行中の曲率に合わせてバウンズを取り直す。
        /// 曲率は実機チューニングで変わるので、生成時に焼き込むと追随できず、
        /// 画面に映っている地面がフラスタムカリングで消える。
        /// メッシュを作り直す必要はなく、バウンズの代入だけなので毎フレーム呼べる。
        /// </summary>
        private void UpdateBounds()
        {
            if (_mesh == null || _config == null)
            {
                return;
            }

            var size = _config.GroundSize;

            // カーブの中心はプレイヤーとともに動く。中心が地面の隅にあるときが最悪で、
            // 対角の隅までのXZ距離の2乗は size^2 * 2 になる
            var worstDrop = Mathf.Max(_appliedStrength, 0f) * size * size * 2f;
            _mesh.bounds = new Bounds(
                new Vector3(0f, -worstDrop * 0.5f, 0f),
                new Vector3(size, worstDrop, size));
        }
    }
}
