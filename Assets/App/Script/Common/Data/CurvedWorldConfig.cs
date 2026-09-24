using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// ワールドを水平線状に曲げる見た目の設定。
    /// 曲げているのは描画だけで、コライダー・NavMesh・弾道はすべて平らなままなので、
    /// この設定を変えてもゲームプレイの判定には影響しない。
    ///
    /// 曲率そのものではなく「どこまで見渡せるか」を距離[m]で指定する。
    /// 曲率は距離の2乗に反比例する値で、直接触っても見え方を想像しにくい。
    /// </summary>
    [CreateAssetMenu(fileName = "CurvedWorldConfig", menuName = "Config/CurvedWorldConfig")]
    public class CurvedWorldConfig : ScriptableObject
    {
        /// <summary>見渡せる距離の下限[m]。これより手前で折り返すと足元しか見えない</summary>
        public const float MinHorizonDistance = 15f;

        /// <summary>見渡せる距離の上限[m]。ここまで来ると曲がっているとわからなくなる</summary>
        public const float MaxHorizonDistance = 400f;

        [Header("有効化")]
        [SerializeField, Tooltip("水平線カーブを有効にする。OFFで従来どおりの平らな見た目に戻る")]
        private bool _isEnabled = true;

        [Header("見える範囲")]
        [SerializeField, Range(MinHorizonDistance, MaxHorizonDistance),
         Tooltip("水平線までの距離[m]。この距離で地面が折り返し、その先のものが隠れる。大きいほど球がゆるく、広い範囲が見える")]
        private float _horizonDistance = 110f;

        [Header("カメラ配置")]
        [SerializeField, Range(5f, 120f), Tooltip("プレイヤー足元からカメラまでの高さ[m]。高いほど広い範囲が視界に入る")]
        private float _cameraHeight = 42f;

        [SerializeField, Tooltip("カメラを後ろへ引く距離[m]。プレイヤーを画面の手前寄りに置きたいときに使う")]
        private float _cameraBackOffset = 10f;

        [Header("カリング")]
        [SerializeField, Min(0f),
         Tooltip("レンダラーのバウンズを下へ広げる量[m]。水平線上の沈下量は見える範囲によらずカメラ高さに等しいので、その数倍を取っておけば足りる")]
        private float _cullingDropMargin = 140f;

        [Header("線の分割")]
        [SerializeField, Min(0.001f),
         Tooltip("LineRendererを刻むときに許す、線の中央のたわみ[m]。2点だけの線は両端しか沈まず、間が弦のまま浮く")]
        private float _maxLineSag = 0.1f;

        [Header("実機での調整")]
        [SerializeField, Min(0f), Tooltip("スティック1秒あたりの見える範囲の変化量[m]")]
        private float _horizonChangePerSecond = 30f;

        [Header("地面メッシュ")]
        [SerializeField, Min(1f), Tooltip("生成する地面グリッドの一辺[m]。見える範囲の倍以上にしないと端が見切れる")]
        private float _groundSize = 900f;

        [SerializeField, Range(2, 254), Tooltip("地面グリッドの一辺の分割数。頂点が粗いとカーブがカクつく")]
        private int _groundDivisions = 150;

        public bool IsEnabled => _isEnabled;

        public float HorizonDistance => _horizonDistance;
        public float CameraHeight => _cameraHeight;
        public float CameraBackOffset => _cameraBackOffset;
        public float CullingDropMargin => _cullingDropMargin;
        public float HorizonChangePerSecond => _horizonChangePerSecond;
        public float GroundSize => _groundSize;
        public int GroundDivisions => _groundDivisions;

        /// <summary>設定された見える範囲から求めた曲率</summary>
        public float Strength => CurvedWorldDisplacement.StrengthForHorizon(_cameraHeight, _horizonDistance);

        /// <summary>無効時は0を返すため、シェーダ側に分岐を持たせずに済む</summary>
        public float EffectiveStrength => _isEnabled ? Strength : 0f;

        /// <summary>カメラリグの、プレイヤー足元から見たローカル位置</summary>
        public Vector3 CameraLocalPosition => new(0f, _cameraHeight, -_cameraBackOffset);

        /// <summary>
        /// 線を刻むときに許すたわみ[m]。
        /// これを実際の刻み間隔へ変換するのは CurvedWorldLine で、
        /// そちらは設定値ではなく実行中の曲率を見る。
        /// </summary>
        public float MaxLineSag => _maxLineSag;

    }
}
