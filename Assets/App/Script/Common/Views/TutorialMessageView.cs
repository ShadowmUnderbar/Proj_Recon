using App.Common.Data;
using App.Common.Interface;
using TMPro;
using UnityEngine;
using VContainer;

namespace App.Common.Views
{
    /// <summary>
    /// チュートリアルメッセージ（WorldSpace Canvas）。バトルとメインメニューの双方で使う。
    /// 生成・破棄と Inspector 値だけを持ち、配置の計算と状態遷移は <see cref="TutorialMessagePlacement"/>、
    /// 縮小・展開の状態は <see cref="TutorialMessageFold"/> に任せる。
    /// 追従先の姿勢は UseCase から <see cref="UpdateAnchor"/> で毎フレーム受け取る。
    ///
    /// 非利き手に追従している間は手のひら側に置き、読める面の向きだけを手に固定して手のひらの向こうへ向ける
    /// （面の傾きは頭の上方向に合わせ、文字を水平に保つ）。
    /// 手首を返して手のひらを見た（読める面が頭を向き、かつ視線が当たっている）ときだけ展開し、
    /// それ以外は本文の先頭の数文字（既定5文字）だけに縮め、ダイアログもその文字ぴったりの大きさ（余白なし）にする。
    /// 手元にある間はダイアログ全体を小さくする（既定 0.5 倍）。
    /// 縮小・展開はダイアログの中央を基準に大きさを補間する
    /// </summary>
    public class TutorialMessageView : MonoBehaviour, ITutorialMessageView
    {
        [SerializeField, Tooltip("UI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("本文テキスト。枠は中央基準で、縮小時は先頭の数文字ぴったりの大きさへ縮める")]
        private TextMeshProUGUI _bodyText;

        [Header("視点追従")]
        [SerializeField, Tooltip("表示開始から視点の正面に追従させる時間[s]")]
        private float _headFollowDuration = 3f;

        [SerializeField, Tooltip("視点追従時のオフセット[m]。頭のローカル座標（x:右 y:上 z:前）")]
        private Vector3 _headOffset = new(0f, -0.15f, 1.2f);

        [Header("非利き手追従")]
        [SerializeField, Tooltip("非利き手追従時のオフセット[m]。指し示す向きへ補正した手のローカル座標（x:右 y:上 z:前）。左手向けの値で、右手のときは x を反転する。左手の手のひらは +x 側")]
        private Vector3 _handOffset = new(0.1f, 0f, 0.1f);

        [SerializeField, Tooltip("非利き手追従時に読める面を向ける方向。指し示す向きへ補正した手のローカル座標で、この軸だけ手に固定し、面の傾きは頭の上方向に合わせる。左手向けの値で、右手のときは x を反転する。既定は手のひらの向こう（-x）")]
        private Vector3 _handForward = Vector3.left;

        [SerializeField, Range(0f, 90f), Tooltip("手のひらを見ているとみなす角度[deg]。頭→ダイアログの向きと、読める面の向きとのなす角がこれ以下なら展開できる")]
        private float _facingAngle = 45f;

        [SerializeField, Range(0f, 45f), Tooltip("手のひらを見ている状態から外れるときに判定角度へ足す余白[deg]。境界付近の手ぶれで縮小・展開を繰り返さないようにする")]
        private float _facingExitMargin = 10f;

        [SerializeField, Range(0.1f, 1f), Tooltip("非利き手追従時の大きさの倍率（視点追従時を 1 とする）。手元へ移るときに位置と同じ速さで縮む")]
        private float _handScale = 0.5f;

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

        [SerializeField, Min(1), Tooltip("縮小時に表示する本文の先頭の文字数。ダイアログはこの文字ぴったりの大きさ（余白なし）に縮む")]
        private int _collapsedCharCount = 5;

        private readonly TutorialMessagePlacement _placement = new();
        private readonly TutorialMessageFold _fold = new();

        private RectTransform _canvasRect;

        /// <summary>展開時のダイアログの大きさ[px]（プレハブでの大きさ）</summary>
        private Vector2 _expandedSize;

        /// <summary>展開時の本文の枠の大きさ[px]（プレハブでの大きさ）</summary>
        private Vector2 _expandedBodySize;

        /// <summary>縮小時の本文の枠の大きさ[px]。本文を差し替えるたびに先頭の文字の幅と行の高さから求め直す</summary>
        private Vector2 _collapsedBodySize;

        /// <summary>縮小時に表示する文字数（空白を含む先頭からの数）。表示文字を <see cref="_collapsedCharCount"/> 個含むところまで</summary>
        private int _collapsedVisibleCount;

        /// <summary>展開時の本文の組み方（プレハブの設定）。縮小時は折り返しなし・左上揃え・はみ出し表示へ切り替え、展開で戻す</summary>
        private HorizontalAlignmentOptions _expandedAlignment;

        private VerticalAlignmentOptions _expandedVerticalAlignment;

        private bool _expandedWordWrapping;
        private TextOverflowModes _expandedOverflow;

        /// <summary>プレハブで自動サイズが有効か。本文ごとに展開時の枠で文字サイズを決め、以降は固定する</summary>
        private bool _isAutoSizing;

        /// <summary>本文の枠を縮小時の大きさにしているか。同じ値の再設定でテキストを組み直さないように覚えておく</summary>
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
            _expandedSize = _canvasRect.sizeDelta;
            _expandedBodySize = _bodyText.rectTransform.sizeDelta;
            _isAutoSizing = _bodyText.enableAutoSizing;
            _expandedAlignment = _bodyText.horizontalAlignment;
            _expandedVerticalAlignment = _bodyText.verticalAlignment;
            _expandedWordWrapping = _bodyText.enableWordWrapping;
            _expandedOverflow = _bodyText.overflowMode;

            Hide();
        }

        public void Show(string text)
        {
            _fold.Reset();
            SetText(text);
            _placement.Begin();

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
            transform.localScale = Vector3.one * (_localScale * _placement.Scale);
            _root.SetActive(true);

            // 視点の正面にいる間は常に展開する。手元へ移ってからは、手首を返して手のひらを見ているときだけ展開する
            var isLookingAtPalm = _placement.IsFacingHead && _gazeTarget.IsGazed.CurrentValue;
            var shouldCollapse = _placement.Phase == TutorialMessagePhase.HandFollow && !isLookingAtPalm;
            _fold.Update(Time.unscaledDeltaTime, shouldCollapse, _foldDuration);
            ApplyFold();
        }

        public void Hide()
        {
            _placement.End();
            _root.SetActive(false);
        }

        /// <summary>
        /// 展開時の枠で文字サイズを決めて固定し、縮小時の本文の大きさ（先頭の数文字ぶんの幅と行の高さ）を求める。
        /// 文字サイズを固定するのは、縮小・展開で枠が変わっても自動サイズで文字の大きさが変わらないようにするため
        /// </summary>
        private void MeasureBody()
        {
            // 測るあいだは展開時の組み方にする。次の反映で縮小・展開どちらにも必ず設定し直す
            SetBodyLayout(false);
            _isBodyCollapsed = null;

            _bodyText.enableAutoSizing = _isAutoSizing;
            _bodyText.ForceMeshUpdate(true);
            var fontSize = _bodyText.fontSize;
            _bodyText.enableAutoSizing = false;
            _bodyText.fontSize = fontSize;

            // 縮小時と同じく折り返さず左揃えで組み、先頭から表示文字を数えて幅を測る（色タグなどは文字に数えない）。
            // 数えるのは1行目の文字だけ。1行目が規定の文字数に満たなければ、その文字数だけを表示する（枠が1行ぶんの高さのため）
            _bodyText.enableWordWrapping = false;
            _bodyText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            _bodyText.verticalAlignment = VerticalAlignmentOptions.Top;
            _bodyText.ForceMeshUpdate(true);

            var textInfo = _bodyText.textInfo;
            var left = 0f;
            var right = 0f;
            var visibleCount = 0;
            _collapsedVisibleCount = 0;
            for (var i = 0; i < textInfo.characterCount && visibleCount < _collapsedCharCount; i++)
            {
                var character = textInfo.characterInfo[i];
                if (character.lineNumber > 0)
                {
                    break;
                }

                if (!character.isVisible)
                {
                    continue;
                }

                if (visibleCount == 0)
                {
                    left = character.origin;
                }

                right = character.xAdvance;
                visibleCount++;
                _collapsedVisibleCount = i + 1;
            }

            var height = 0f;
            if (textInfo.lineCount > 0)
            {
                var firstLine = textInfo.lineInfo[0];
                height = firstLine.ascender - firstLine.descender;
            }

            _collapsedBodySize = Vector2.Min(new Vector2(right - left, height), _expandedBodySize);
            SetBodyLayout(false);
        }

        /// <summary>縮小の進み具合をダイアログと本文の大きさへ反映する</summary>
        private void ApplyFold()
        {
            // 縮小しきったときは余白なしで、本文の先頭の文字ぴったりの大きさにする
            _canvasRect.sizeDelta = Vector2.Lerp(_expandedSize, _collapsedBodySize, _fold.Progress);

            if (_isBodyCollapsed == _fold.IsTextCollapsed)
            {
                return;
            }

            _isBodyCollapsed = _fold.IsTextCollapsed;
            SetBodyLayout(_fold.IsTextCollapsed);
        }

        /// <summary>
        /// 本文の枠と組み方を縮小・展開に合わせる。縮小時は折り返さずに左上揃えで組み、先頭の数文字だけを表示する。
        /// 本文に改行があっても1行目が枠の上端に来るよう上揃えにする（中央揃えだと複数行の塊ごと中央に置かれ、1行目が枠の上へずれる）。
        /// 枠をはみ出した残りは表示文字数で隠すので、省略記号は付けない
        /// </summary>
        private void SetBodyLayout(bool isCollapsed)
        {
            SetBodySize(isCollapsed ? _collapsedBodySize : _expandedBodySize);
            _bodyText.enableWordWrapping = !isCollapsed && _expandedWordWrapping;
            _bodyText.horizontalAlignment = isCollapsed ? HorizontalAlignmentOptions.Left : _expandedAlignment;
            _bodyText.verticalAlignment = isCollapsed ? VerticalAlignmentOptions.Top : _expandedVerticalAlignment;
            _bodyText.overflowMode = isCollapsed ? TextOverflowModes.Overflow : _expandedOverflow;
            _bodyText.maxVisibleCharacters = isCollapsed ? _collapsedVisibleCount : int.MaxValue;
        }

        private void SetBodySize(Vector2 size)
        {
            _bodyText.rectTransform.sizeDelta = size;

            // TMP は非表示（非アクティブ）の間に枠が変わっても内部の枠の大きさを取り込み直さず、ForceMeshUpdate でも更新しない。
            // 縮小表示のまま隠したあとの再表示で、縮小時の小さい枠のまま自動サイズが走って最小サイズになるのを防ぐ
            _bodyText.ComputeMarginSize();
        }

        private TutorialMessagePlacementSettings BuildSettings()
        {
            return new TutorialMessagePlacementSettings(
                _headFollowDuration, _headOffset, _handOffset, _handForward, _facingAngle,
                _facingExitMargin, _handScale, _followSpeed);
        }
    }
}
