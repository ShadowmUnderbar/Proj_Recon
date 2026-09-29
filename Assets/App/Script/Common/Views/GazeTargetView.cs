using R3;
using UnityEngine;
using VContainer;

namespace App.Common.Views
{
    /// <summary>
    /// 視線で見られているかを知りたいオブジェクトに付ける注視対象。
    /// 判定の形（球の中心・半径）とちらつき抑制の設定を Inspector で持ち、結果を <see cref="IsGazed"/> で公開する。
    /// 判定そのものは <see cref="GazeTargetStoreView"/> が LateUpdate で順番に呼ぶ（自分では Update しない）。
    /// 有効な間だけ Store に登録し、無効になったら登録を外して「見ていない」に戻す。
    ///
    /// Store は [Inject] で受け取る。VContainer はプレハブの子コンポーネントまでは注入しないため、
    /// プレハブ内に置く場合は持ち主の View が IObjectResolver.InjectGameObject などで注入すること
    /// </summary>
    public class GazeTargetView : MonoBehaviour
    {
        [Header("判定の形")]
        [SerializeField, Tooltip("判定球の中心のオフセット[m]。このTransformのローカル軸（x:右 y:上 z:前）。スケールは反映しない")]
        private Vector3 _centerOffset;

        [SerializeField, Min(0f), Tooltip("判定球の半径[m]。スケールは反映しないので、見た目を拡縮しても判定の大きさは変わらない")]
        private float _radius = 0.2f;

        [Header("ちらつき抑制")]
        [SerializeField, Min(0f), Tooltip("見ていない状態から「見た」とみなす余白角度[度]。球の縁からこの角度まで外れていても当たりとする")]
        private float _enterMarginAngle = 2f;

        [SerializeField, Min(0f), Tooltip("見ている状態から「外した」とみなす余白角度[度]。入りより大きくして境目でのちらつきを防ぐ")]
        private float _exitMarginAngle = 5f;

        [SerializeField, Min(0f), Tooltip("視線が当たり続けてから「見た」に切り替わるまでの時間[s]")]
        private float _enterDelay = 0.15f;

        [SerializeField, Min(0f), Tooltip("視線が外れ続けてから「外した」に切り替わるまでの時間[s]")]
        private float _exitDelay = 0.5f;

        private readonly GazeDetector _detector = new();
        private readonly ReactiveProperty<bool> _isGazed = new(false);

        private IGazeTargetStoreView _store;
        private bool _isRegistered;

        /// <summary>前回判定した時刻（unscaledTime）。Store は毎フレーム全員を判定しないため、経過時間はここから求める</summary>
        private float _lastEvaluatedTime;

        /// <summary>視線で見られているか（遅延・ヒステリシス適用後）。無効な間は false</summary>
        public ReadOnlyReactiveProperty<bool> IsGazed => _isGazed;

        [Inject]
        public void Construct(IGazeTargetStoreView store)
        {
            _store = store;

            // 注入より先に OnEnable が済んでいた場合はここで登録する
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void Start()
        {
            if (_store == null)
            {
                Debug.LogWarning($"[{nameof(GazeTargetView)}] {name}: Store が注入されていないため注視判定されません", this);
            }
        }

        private void OnDisable()
        {
            Unregister();
            _detector.Reset();
            _isGazed.Value = false;
        }

        private void OnDestroy()
        {
            _isGazed.Dispose();
        }

        /// <summary>Store から呼ばれ、頭の姿勢で判定を更新する。time は Time.unscaledTime</summary>
        public void Evaluate(in Pose gaze, float time)
        {
            var elapsed = time - _lastEvaluatedTime;
            _lastEvaluatedTime = time;

            var center = transform.position + transform.rotation * _centerOffset;
            _isGazed.Value = _detector.Update(elapsed, gaze, center, _radius, BuildSettings());
        }

        private void Register()
        {
            if (_store == null || _isRegistered)
            {
                return;
            }

            _store.Register(this);
            _isRegistered = true;

            // 最初の判定の経過時間を登録時点から数え、無効だった間の時間を遅延に含めない
            _lastEvaluatedTime = Time.unscaledTime;
        }

        private void Unregister()
        {
            if (!_isRegistered)
            {
                return;
            }

            _store.Unregister(this);
            _isRegistered = false;
        }

        private GazeDetectorSettings BuildSettings()
        {
            return new GazeDetectorSettings(_enterMarginAngle, _exitMarginAngle, _enterDelay, _exitDelay);
        }
    }
}
