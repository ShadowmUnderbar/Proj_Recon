using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// CurvedWorld.hlsl と同じ沈下量をC#側で求める。
    /// HUDマーカーやワールド座標からの画面変換は頂点シェーダを通らないため、
    /// 同じ式をここで再現しないと表示だけが元の位置に残る。
    /// シェーダ側の式を変えたときは必ず両方を合わせること。
    /// </summary>
    public readonly struct CurvedWorldDisplacement
    {
        private readonly Vector3 _origin;
        private readonly float _strength;

        public CurvedWorldDisplacement(Vector3 origin, float strength)
        {
            _origin = origin;
            _strength = strength;
        }

        public Vector3 Origin => _origin;
        public float Strength => _strength;

        /// <summary>設定と中心座標から生成する</summary>
        public static CurvedWorldDisplacement FromConfig(CurvedWorldConfig config, Vector3 origin)
        {
            return new CurvedWorldDisplacement(origin, config.EffectiveStrength);
        }

        /// <summary>この座標が見た目上どれだけ沈むか[m]</summary>
        public float DropAt(Vector3 positionWS)
        {
            var deltaX = positionWS.x - _origin.x;
            var deltaZ = positionWS.z - _origin.z;
            return (deltaX * deltaX + deltaZ * deltaZ) * _strength;
        }

        /// <summary>論理座標を、画面に描かれている見た目の座標へ変換する</summary>
        public Vector3 Apply(Vector3 positionWS)
        {
            positionWS.y -= DropAt(positionWS);
            return positionWS;
        }

        /// <summary>
        /// 水平線までの距離[m]。これより遠くの地面は画面上で折り返し、後ろのものを隠す。
        /// カメラをほぼ中心の真上に置いたときの、見かけの仰角 (-strength * d - height / d) が
        /// 極値を取る点なので、高さ h に対して sqrt(h / strength) になる。
        /// </summary>
        public float EstimateHorizonDistance(float cameraHeight)
        {
            if (_strength <= Mathf.Epsilon || cameraHeight <= 0f)
            {
                return float.PositiveInfinity;
            }

            return Mathf.Sqrt(cameraHeight / _strength);
        }

        /// <summary>水平線を距離 horizonDistance に置きたいときの曲率</summary>
        public static float StrengthForHorizon(float cameraHeight, float horizonDistance)
        {
            if (horizonDistance <= Mathf.Epsilon)
            {
                return CurvedWorldConfig.MaxStrength;
            }

            return cameraHeight / (horizonDistance * horizonDistance);
        }
    }
}
