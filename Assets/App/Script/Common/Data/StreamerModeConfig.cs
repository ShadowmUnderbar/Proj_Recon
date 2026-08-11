using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// ストリーマーモード（配信用カメラ）の出力設定。
    /// HMDへ出る映像には一切影響せず、PCディスプレイ側の映像だけを差し替えるための設定を持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "StreamerModeConfig", menuName = "Config/StreamerModeConfig")]
    public class StreamerModeConfig : ScriptableObject
    {
        [Header("有効化")]
        [SerializeField, Tooltip("ストリーマーモードを有効にする。無効時は配信用カメラを生成しない")]
        private bool _isEnabled;

        [SerializeField, Tooltip("VR実行時のみ有効にする。非VR（PCデバッグ）ではカメラが1つしかないため通常はON")]
        private bool _vrOnly = true;

        [Header("出力先")]
        [SerializeField, Tooltip("配信用カメラの出力ディスプレイ番号。0=PCメインウィンドウ（ミラー表示を置換）")]
        private int _targetDisplay;

        [SerializeField, Tooltip("XRのミラー表示（HMD映像のPC画面への転送）を抑制する。0番ディスプレイへ出す場合はON")]
        private bool _suppressMirrorView = true;

        [SerializeField, Tooltip("カメラのDepth。プレイヤーカメラ（0）より大きい値にして手前に描画する")]
        private float _cameraDepth = 10f;

        [Header("描画")]
        [SerializeField, Tooltip("配信用カメラが描画するレイヤー。ワールドUIなど配信に映したくないレイヤーは外す")]
        private LayerMask _cullingMask = ~0;

        [SerializeField, Tooltip("配信映像の視野角（度）。プレイヤー視点追従中もこの値を使う")]
        private float _fieldOfView = 70f;

        [Header("プレイヤー視点追従")]
        [SerializeField, Tooltip("追従の平滑化にかける時間（秒）。0で完全追従（HMDの動きをそのまま反映）")]
        private float _followSmoothTime;

        public bool IsEnabled => _isEnabled;
        public bool VROnly => _vrOnly;
        public int TargetDisplay => _targetDisplay;
        public bool SuppressMirrorView => _suppressMirrorView;
        public float CameraDepth => _cameraDepth;
        public LayerMask CullingMask => _cullingMask;
        public float FieldOfView => _fieldOfView;
        public float FollowSmoothTime => _followSmoothTime;
    }
}
