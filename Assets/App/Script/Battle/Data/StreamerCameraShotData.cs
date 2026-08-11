using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 配信用カメラの演出ショット1種の定義。
    /// 「どこから・どの画角で・何秒撮って戻るか」を全てここに外部化する。
    /// </summary>
    [CreateAssetMenu(fileName = "StreamerCameraShotData", menuName = "Config/StreamerCameraShotData")]
    public class StreamerCameraShotData : ScriptableObject
    {
        [Header("識別")]
        [SerializeField, Tooltip("クールダウン管理に使うID。重複しない文字列にする")]
        private string _shotId = "shot";

        [SerializeField, Tooltip("構図の種別")]
        private StreamerCameraShotType _shotType = StreamerCameraShotType.Static;

        [Header("配置")]
        [SerializeField, Tooltip("被写体の中心からカメラまでの水平距離（m）")]
        private float _distance = 6f;

        [SerializeField, Tooltip("被写体の中心からの高さオフセット（m）")]
        private float _height = 2f;

        [SerializeField, Tooltip("プレイヤーから見た被写体方向を基準にした水平角オフセット（度）")]
        private float _yawOffset = 35f;

        [SerializeField, Tooltip("Orbit時の回り込み速度（度/秒）")]
        private float _orbitSpeed = 20f;

        [SerializeField, Tooltip("注視点の高さオフセット（m）。被写体の足元でなく胸あたりを狙う用")]
        private float _lookAtHeight = 1f;

        [Header("画角")]
        [SerializeField, Tooltip("演出中の視野角（度）")]
        private float _fieldOfView = 55f;

        [SerializeField, Tooltip("被写体が複数のとき、全員が収まるようDistanceを自動で伸ばす")]
        private bool _useAutoFraming = true;

        [SerializeField, Tooltip("自動フレーミング時の余白倍率。1.0で被写体ぴったり")]
        private float _framingMargin = 1.3f;

        [Header("尺")]
        [SerializeField, Tooltip("プレイヤー視点から演出画角へ寄る時間（秒）")]
        private float _blendInDuration = 0.35f;

        [SerializeField, Tooltip("演出画角を保持する時間（秒）")]
        private float _holdDuration = 1.2f;

        [SerializeField, Tooltip("プレイヤー視点へ戻る時間（秒）")]
        private float _blendOutDuration = 0.5f;

        [Header("発動制御")]
        [SerializeField, Tooltip("値が大きいほど優先。再生中のショットより高い場合のみ割り込める")]
        private int _priority = 10;

        [SerializeField, Tooltip("このショットの再連発を防ぐクールダウン（秒）")]
        private float _cooldownSeconds = 8f;

        public string ShotId => _shotId;
        public StreamerCameraShotType ShotType => _shotType;
        public float Distance => _distance;
        public float Height => _height;
        public float YawOffset => _yawOffset;
        public float OrbitSpeed => _orbitSpeed;
        public float LookAtHeight => _lookAtHeight;
        public float FieldOfView => _fieldOfView;
        public bool UseAutoFraming => _useAutoFraming;
        public float FramingMargin => _framingMargin;
        public float BlendInDuration => Mathf.Max(0f, _blendInDuration);
        public float HoldDuration => Mathf.Max(0f, _holdDuration);
        public float BlendOutDuration => Mathf.Max(0f, _blendOutDuration);
        public int Priority => _priority;
        public float CooldownSeconds => Mathf.Max(0f, _cooldownSeconds);

        /// <summary>ブレンドインから復帰完了までの総尺（秒）</summary>
        public float TotalDuration => BlendInDuration + HoldDuration + BlendOutDuration;
    }
}
