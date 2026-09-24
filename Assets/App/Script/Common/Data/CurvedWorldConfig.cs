using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// ワールドを水平線状に曲げる見た目の設定。
    /// 曲げているのは描画だけで、コライダー・NavMesh・弾道はすべて平らなままなので、
    /// この設定を変えてもゲームプレイの判定には影響しない。
    /// </summary>
    [CreateAssetMenu(fileName = "CurvedWorldConfig", menuName = "Config/CurvedWorldConfig")]
    public class CurvedWorldConfig : ScriptableObject
    {
        /// <summary>曲率を無効にしたときに渡す値。0なら沈下量が常に0になる</summary>
        public const float MinStrength = 0f;

        /// <summary>曲率の上限。カメラ高さ18.6mで水平線が19mまで寄る。これ以上は足元しか見えない</summary>
        public const float MaxStrength = 0.05f;

        [Header("有効化")]
        [SerializeField, Tooltip("水平線カーブを有効にする。OFFで従来どおりの平らな見た目に戻る")]
        private bool _isEnabled = true;

        [Header("曲率")]
        [SerializeField, Range(MinStrength, MaxStrength), Tooltip("中心からの距離の2乗にかける係数。水平線までの距離はsqrt(カメラ高さ / この値)")]
        private float _strength = 0.01f;

        [SerializeField, Min(0f), Tooltip("カリング用にレンダラーのバウンズを下へ広げる量[m]。水平線上の沈下量は曲率によらずカメラ高さに等しいので、その数倍を取っておけば足りる")]
        private float _cullingDropMargin = 60f;

        [Header("実機での調整")]
        [SerializeField, Min(0f), Tooltip("スティック1秒あたりの曲率の変化量。実機で強度を振るときの速さ")]
        private float _strengthChangePerSecond = 0.002f;

        [Header("地面メッシュ")]
        [SerializeField, Min(1f), Tooltip("生成する地面グリッドの一辺[m]。ステージ全体を覆う大きさにする")]
        private float _groundSize = 300f;

        [SerializeField, Range(2, 254), Tooltip("地面グリッドの一辺の分割数。頂点が粗いとカーブがカクつく")]
        private int _groundDivisions = 120;

        public bool IsEnabled => _isEnabled;

        /// <summary>無効時は0を返すため、シェーダ側に分岐を持たせずに済む</summary>
        public float EffectiveStrength => _isEnabled ? _strength : MinStrength;

        public float Strength => _strength;
        public float CullingDropMargin => _cullingDropMargin;
        public float StrengthChangePerSecond => _strengthChangePerSecond;
        public float GroundSize => _groundSize;
        public int GroundDivisions => _groundDivisions;
    }
}
