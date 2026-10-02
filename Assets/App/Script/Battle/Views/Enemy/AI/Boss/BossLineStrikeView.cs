using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// ボスの帯状の攻撃の予兆・攻撃の表示（地面に置く半透明の帯）。
    /// 水平線カーブ（CurvedWorldUnlit・頂点ごと）で地面に沿わせるため、長さ方向に細かく刻んだメッシュを生成する。
    /// ボスの拡大率や回転の影響を受けないよう、ボスの子にはせず単独のオブジェクトとして置く。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BossLineStrikeView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Min(0.1f), Tooltip("長さ方向の刻み間隔（m）。粗いと水平線カーブで帯の途中が地面から浮く")]
        private float _segmentLength = 0.5f;

        [SerializeField, Tooltip("地面からの浮かせ量（m）。地面とのZファイティングを防ぐ")]
        private float _heightOffset = 0.03f;

        [SerializeField, Min(0f), Tooltip("バウンズを下へ広げる量（m）。頂点が沈んでもカリングで消えないよう、カメラ高さの数倍にする")]
        private float _boundsDownMargin = 60f;

        private MeshRenderer _renderer;
        private Mesh _mesh;
        private MaterialPropertyBlock _propertyBlock;
        private float _builtLength = -1f;
        private float _builtWidth = -1f;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();

            // DontSaveを付けないと、生成した頂点がシーンへ焼き込まれる
            _mesh = new Mesh { name = "BossLineStrike", hideFlags = HideFlags.DontSave };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer.enabled = false;
        }

        /// <summary>
        /// origin から direction へ伸びる帯を表示する（水平方向のみ使う）
        /// </summary>
        public void Show(Vector3 origin, Vector3 direction, float length, float width, Color color)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            if (!Mathf.Approximately(length, _builtLength) || !Mathf.Approximately(width, _builtWidth))
            {
                BuildMesh(length, width);
            }

            transform.SetPositionAndRotation(new Vector3(origin.x, _heightOffset, origin.z),
                Quaternion.LookRotation(direction.normalized, Vector3.up));
            SetColor(color);
            _renderer.enabled = true;
        }

        public void SetColor(Color color)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        public void Hide()
        {
            _renderer.enabled = false;
        }

        public bool IsVisible => _renderer != null && _renderer.enabled;

        /// <summary>ローカルの +Z へ length、X に ±width/2 の帯を、長さ方向に刻んで作る</summary>
        private void BuildMesh(float length, float width)
        {
            _builtLength = length;
            _builtWidth = width;

            var segments = Mathf.Max(1, Mathf.CeilToInt(length / _segmentLength));
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            var halfWidth = width * 0.5f;

            for (var i = 0; i <= segments; i++)
            {
                var t = (float)i / segments;
                var z = length * t;
                vertices[i * 2] = new Vector3(-halfWidth, 0f, z);
                vertices[i * 2 + 1] = new Vector3(halfWidth, 0f, z);
                uvs[i * 2] = new Vector2(0f, t);
                uvs[i * 2 + 1] = new Vector2(1f, t);
            }

            for (var i = 0; i < segments; i++)
            {
                var v = i * 2;
                var tri = i * 6;
                triangles[tri] = v;
                triangles[tri + 1] = v + 2;
                triangles[tri + 2] = v + 1;
                triangles[tri + 3] = v + 1;
                triangles[tri + 4] = v + 2;
                triangles[tri + 5] = v + 3;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();

            // 頂点はシェーダで沈むが、カリングは CPU 側のバウンズで行うため下へ広げておく
            var bounds = new Bounds(new Vector3(0f, 0f, length * 0.5f), new Vector3(width, 0f, length));
            bounds.Encapsulate(new Vector3(0f, -_boundsDownMargin, length * 0.5f));
            _mesh.bounds = bounds;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
