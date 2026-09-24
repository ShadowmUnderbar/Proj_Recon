using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 水平線カーブの中心とパラメータを、毎フレームすべてのシェーダへ配る。
    /// カーブの中心はカメラではなくプレイヤーの足元に置く。
    /// VRではHMDが小刻みに動くため、カメラを中心にすると首を振るたびに地形が波打つ。
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public class CurvedWorldView : MonoBehaviour
    {
        /// <summary>他のViewが位置を確定した後に中心を配りたいので、既定より後ろで回す</summary>
        private const int ExecutionOrder = 100;

        private static readonly int OriginId = Shader.PropertyToID("_CurvedWorldOrigin");
        private static readonly int ParamsId = Shader.PropertyToID("_CurvedWorldParams");

        [SerializeField, Tooltip("見える範囲・カメラ配置・地面グリッドの設定")]
        private CurvedWorldConfig _config;

        [SerializeField, Tooltip("カーブの中心にする対象。通常はプレイヤーの足元。未指定ならこのオブジェクト自身")]
        private Transform _origin;

        /// <summary>
        /// 実行中の「見渡せる距離」[m]。設定アセットの値を起点にして、実機調整はこちらだけを書き換える。
        /// アセットを直接書き換えると、Playを抜けたあとも値が残って意図しない差分になる。
        /// </summary>
        private float _runtimeHorizonDistance;

        /// <summary>現在フレームの変位。HUDなど他のViewが同じ変位を参照するために公開する</summary>
        public CurvedWorldDisplacement Displacement { get; private set; }

        public CurvedWorldConfig Config => _config;

        /// <summary>実行中の見渡せる距離[m]。実機で調整した値を設定アセットへ書き戻すときに読む</summary>
        public float RuntimeHorizonDistance => _runtimeHorizonDistance;

        private Transform OriginTransform => _origin != null ? _origin : transform;

        private void OnEnable()
        {
            _runtimeHorizonDistance = _config != null ? _config.HorizonDistance : CurvedWorldConfig.MaxHorizonDistance;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void OnDisable()
        {
            // 曲率0を配ってから止める。止めた瞬間に最後の値が残り続けるのを防ぐ
            Displacement = new CurvedWorldDisplacement(OriginTransform.position, 0f);
            PushToShaders(Displacement, 0f);
        }

        /// <summary>実行中の見渡せる距離を増減する。設定アセットには触れない</summary>
        public void AdjustHorizonDistance(float deltaMeters)
        {
            _runtimeHorizonDistance = Mathf.Clamp(
                _runtimeHorizonDistance + deltaMeters,
                CurvedWorldConfig.MinHorizonDistance,
                CurvedWorldConfig.MaxHorizonDistance);
        }

        private void Apply()
        {
            if (_config == null)
            {
                return;
            }

            var strength = _config.IsEnabled
                ? CurvedWorldDisplacement.StrengthForHorizon(_config.CameraHeight, _runtimeHorizonDistance)
                : 0f;

            Displacement = new CurvedWorldDisplacement(OriginTransform.position, strength);
            PushToShaders(Displacement, _config.MaxLineSag);
        }

        /// <summary>
        /// yには線の許容たわみを載せる。シェーダは読まないが、CurvedWorldLineが
        /// 「いま実際に配られている曲率」から刻み間隔を出すのに要る。
        /// 設定アセット側を読ませると、実機で曲率を振ったとき刻みが追随しない。
        /// </summary>
        private static void PushToShaders(CurvedWorldDisplacement displacement, float maxLineSag)
        {
            var origin = displacement.Origin;
            Shader.SetGlobalVector(OriginId, new Vector4(origin.x, origin.y, origin.z, 0f));
            Shader.SetGlobalVector(ParamsId, new Vector4(displacement.Strength, maxLineSag, 0f, 0f));
        }
    }
}
