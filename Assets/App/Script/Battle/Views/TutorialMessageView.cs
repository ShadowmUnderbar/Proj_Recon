using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Views;
using TMPro;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// バトル中のチュートリアルメッセージ（WorldSpace Canvas）。
    /// 生成・破棄と Inspector 値だけを持ち、配置の計算と状態遷移は <see cref="TutorialMessagePlacement"/> に任せる。
    /// 追従先の姿勢は UseCase から <see cref="UpdateAnchor"/> で毎フレーム受け取る
    /// </summary>
    public class TutorialMessageView : MonoBehaviour, ITutorialMessageView
    {
        [SerializeField, Tooltip("UI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("本文テキスト")]
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

        private readonly TutorialMessagePlacement _placement = new();

        public TutorialMessagePhase Phase => _placement.Phase;

        private void Awake()
        {
            Hide();
        }

        public void Show(string text)
        {
            SetText(text);
            _placement.Begin();
            transform.localScale = Vector3.one * _localScale;

            // 姿勢は次の UpdateAnchor で決まり、そこで表示される。
            // 非表示から出すときは配置されるまで隠れたまま、表示中の差し替えなら消えずに新しい位置へ移る
        }

        public void SetText(string text)
        {
            _bodyText.text = text;
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
        }

        public void Hide()
        {
            _placement.End();
            _root.SetActive(false);
        }

        private TutorialMessagePlacementSettings BuildSettings()
        {
            return new TutorialMessagePlacementSettings(_headFollowDuration, _headOffset, _handOffset, _followSpeed);
        }
    }
}
