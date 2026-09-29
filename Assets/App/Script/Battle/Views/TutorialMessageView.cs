using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Views;
using TMPro;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// バトル中のチュートリアルメッセージ（WorldSpace Canvas）。
    /// 生成・破棄と Inspector 値だけを持ち、配置の計算と状態遷移は <see cref="TutorialMessagePlacement"/>、
    /// 縮小・展開の状態は <see cref="TutorialMessageFold"/> に任せる。
    /// 追従先の姿勢は UseCase から <see cref="UpdateAnchor"/> で毎フレーム受け取る。
    ///
    /// 非利き手に追従している間、視線が当たっていなければ表示上の1行目だけに縮める（続きがあれば末尾を「…」にする）。
    /// 縮小・展開はダイアログの中央を基準に高さを補間する
    /// </summary>
    public class TutorialMessageView : MonoBehaviour, ITutorialMessageView
    {
        /// <summary>
        /// 縮小時の本文の枠に足す余裕[px]。1行目の高さぴったりだと丸め誤差で1行目まで省略されることがあるため。
        /// 2行目が入るほどの大きさではないので、1行目だけが残る
        /// </summary>
        private const float CollapsedLineTolerance = 1f;

        [SerializeField, Tooltip("UI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("本文テキスト。枠は中央基準で、縮小時は1行目の高さへ縮める")]
        private TextMeshProUGUI _bodyText;

        [Header("視点追従")]
        [SerializeField, Tooltip("表示開始から視点の正面に追従させる時間[s]")]
        private float _headFollowDuration = 3f;

        [SerializeField, Tooltip("視点追従時のオフセット[m]。頭のローカル座標（x:右 y:上 z:前）")]
        private Vector3 _headOffset = new(0f, -0.15f, 1.2f);

        [Header("非利き手追従")]
        [SerializeField, Tooltip("非利き手追従時のオフセット[m]。指し示す向きへ補正した手のローカル座標（x:右 y:上 z:前）。左手向けの値で、右手のときは x を反転する")]
        private Vector3 _handOffset = new(-0.1f, 0.2f, 0.1f);

        [Header("追従")]
        [SerializeField, Tooltip("定位置へ追いつく速さ。大きいほど速く、0以下なら補間せず即座に置く")]
        private float _followSpeed = 6f;

        [SerializeField, Tooltip("Canvasのスケール。1px＝何mかを表す")]
        private float _localScale = 0.001f;

        [Header("縮小表示")]
        [SerializeField, Tooltip("視線判定の対象。非表示の間は判定から外れるよう、表示切替するルートに付ける")]
        private GazeTargetView _gazeTarget;

        [SerializeField, Tooltip("縮小・展開にかける時間[s]。0以下なら即座に切り替える")]
        private float _foldDuration = 0.15f;

        private readonly TutorialMessagePlacement _placement = new();
        private readonly TutorialMessageFold _fold = new();

        private RectTransform _canvasRect;

        /// <summary>展開時のダイアログの高さ[px]（プレハブでの大きさ）</summary>
        private float _expandedHeight;

        /// <summary>展開時の本文の枠の高さ[px]（プレハブでの大きさ）</summary>
        private float _expandedBodyHeight;

        /// <summary>縮小時の本文の枠の高さ[px]。本文を差し替えるたびに1行目の高さから求め直す</summary>
        private float _collapsedBodyHeight;

        /// <summary>プレハブで自動サイズが有効か。本文ごとに展開時の枠で文字サイズを決め、以降は固定する</summary>
        private bool _isAutoSizing;

        /// <summary>本文の枠を1行目の高さにしているか。同じ値の再設定でテキストを組み直さないように覚えておく</summary>
        private bool? _isBodyCollapsed;

        public TutorialMessagePhase Phase => _placement.Phase;

        [Inject]
        public void Construct(IGazeTargetStoreView gazeTargetStore)
        {
            // VContainer はプレハブのルートのコンポーネントにしか注入しないため、子の注視対象へはここから渡す
            _gazeTarget.Construct(gazeTargetStore);
        }

        private void Awake()
        {
            _canvasRect = (RectTransform)transform;
            _expandedHeight = _canvasRect.sizeDelta.y;
            _expandedBodyHeight = _bodyText.rectTransform.sizeDelta.y;
            _isAutoSizing = _bodyText.enableAutoSizing;

            Hide();
        }

        public void Show(string text)
        {
            _fold.Reset();
            SetText(text);
            _placement.Begin();
            transform.localScale = Vector3.one * _localScale;

            // 姿勢は次の UpdateAnchor で決まり、そこで表示される。
            // 非表示から出すときは配置されるまで隠れたまま、表示中の差し替えなら消えずに新しい位置へ移る
        }

        public void SetText(string text)
        {
            _bodyText.text = text;
            MeasureBody();
            ApplyFold();
        }

        public void UpdateAnchor(TutorialMessageAnchor anchor)
        {
            // コントローラの前方は寝ているため、指し示す向きへ起こした姿勢を基準にする
            var adjustedAnchor = new TutorialMessageAnchor(
                anchor.HeadPose, anchor.Hand, PlatformHandRotation.ToPointingPose(anchor.HandPose), anchor.IsHandAvailable);

            if (!_placement.TryUpdate(Time.unscaledDeltaTime, adjustedAnchor, BuildSettings(), out var pose))
            {
                return;
            }

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _root.SetActive(true);

            // 視点の正面にいる間は常に展開する。手元へ移ってからは、見ていなければ縮める
            var shouldCollapse = _placement.Phase == TutorialMessagePhase.HandFollow && !_gazeTarget.IsGazed.CurrentValue;
            _fold.Update(Time.unscaledDeltaTime, shouldCollapse, _foldDuration);
            ApplyFold();
        }

        public void Hide()
        {
            _placement.End();
            _root.SetActive(false);
        }

        /// <summary>
        /// 展開時の枠で文字サイズを決めて固定し、縮小時の本文の高さ（1行目の高さ）を求める。
        /// 文字サイズを固定するのは、縮小・展開で枠が変わっても自動サイズで文字の大きさが変わらないようにするため
        /// </summary>
        private void MeasureBody()
        {
            // 測るあいだは展開時の枠にする。次の反映で縮小・展開どちらの枠にも必ず設定し直す
            SetBodyHeight(_expandedBodyHeight);
            _isBodyCollapsed = null;

            _bodyText.enableAutoSizing = _isAutoSizing;
            _bodyText.ForceMeshUpdate(true);
            var fontSize = _bodyText.fontSize;
            _bodyText.enableAutoSizing = false;
            _bodyText.fontSize = fontSize;
            _bodyText.ForceMeshUpdate(true);

            var textInfo = _bodyText.textInfo;
            if (textInfo.lineCount == 0)
            {
                _collapsedBodyHeight = 0f;
                return;
            }

            var firstLine = textInfo.lineInfo[0];
            _collapsedBodyHeight = firstLine.ascender - firstLine.descender + CollapsedLineTolerance;
        }

        /// <summary>縮小の進み具合をダイアログと本文の大きさへ反映する</summary>
        private void ApplyFold()
        {
            var bodyPadding = _expandedHeight - _expandedBodyHeight;
            var collapsedHeight = Mathf.Min(_collapsedBodyHeight + bodyPadding, _expandedHeight);
            var size = _canvasRect.sizeDelta;
            size.y = Mathf.Lerp(_expandedHeight, collapsedHeight, _fold.Progress);
            _canvasRect.sizeDelta = size;

            if (_isBodyCollapsed == _fold.IsTextCollapsed)
            {
                return;
            }

            _isBodyCollapsed = _fold.IsTextCollapsed;
            SetBodyHeight(_fold.IsTextCollapsed ? Mathf.Min(_collapsedBodyHeight, _expandedBodyHeight) : _expandedBodyHeight);
        }

        private void SetBodyHeight(float height)
        {
            var bodyRect = _bodyText.rectTransform;
            var size = bodyRect.sizeDelta;
            size.y = height;
            bodyRect.sizeDelta = size;
        }

        private TutorialMessagePlacementSettings BuildSettings()
        {
            return new TutorialMessagePlacementSettings(_headFollowDuration, _headOffset, _handOffset, _followSpeed);
        }
    }
}
