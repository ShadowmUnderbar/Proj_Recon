using App.Battle.Data;
using App.Common.Data;
using App.Common.Data.MasterData;
using TMPro;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// アップグレード候補1件ぶんの3Dカード。
    /// 表示内容の反映と、定位置（ホームポーズ）への復帰・掴んだ手への追従といった見た目の更新だけを担当する。
    /// どのカードを掴んでいるか等の状態管理は <see cref="UpgradeCardBoardView"/> 側が持つ
    /// </summary>
    public class UpgradeCardView : MonoBehaviour
    {
        [SerializeField, Tooltip("アップグレード名（ローカライズ: $Name）")]
        private TextMeshPro _nameText;

        [SerializeField, Tooltip("レベル表記（ローカライズ: $Level{n}_Upgrade）")]
        private TextMeshPro _levelText;

        [SerializeField, Tooltip("簡略説明（ローカライズ: $Name_SimpleDesc）")]
        private TextMeshPro _simpleDescriptionText;

        [SerializeField, Tooltip("詳細説明。効果値は埋め込み済み（ローカライズ: $Name_Desc）")]
        private TextMeshPro _descriptionText;

        [SerializeField, Tooltip("購入コストの表記")]
        private TextMeshPro _costText;

        [SerializeField, Tooltip("掴み判定に使うコライダー")]
        private Collider _collider;

        [SerializeField, Tooltip("右手で持つときの持ち手。カードの右端に置き、手に合わせたい位置・角度でTransformを調整する")]
        private Transform _rightHandGripAnchor;

        [SerializeField, Tooltip("左手で持つときの持ち手。カードの左端に置き、手に合わせたい位置・角度でTransformを調整する")]
        private Transform _leftHandGripAnchor;

        [SerializeField, Tooltip("掴んでいるときに色を変える枠のレンダラー（任意）")]
        private Renderer _frameRenderer;

        [SerializeField, Tooltip("通常時の枠色")]
        private Color _defaultColor = Color.white;

        [SerializeField, Tooltip("掴んでいるときの枠色")]
        private Color _heldColor = Color.cyan;

        [SerializeField, Tooltip("レイで狙っているときの枠色")]
        private Color _hoveredColor = Color.yellow;

        [SerializeField, Tooltip("ポイントが足りず買えないときの枠色・コスト文字色")]
        private Color _unpurchasableColor = new(0.45f, 0.45f, 0.45f);

        /// <summary>持ち手が未設定のときに使う、手からカードまでのオフセット[m]</summary>
        private static readonly Vector3 FallbackHoldOffset = new(0f, 0.02f, 0.12f);

        /// <summary>枠色の変更に使うマテリアルプロパティ名</summary>
        private static readonly int ColorPropertyId = Shader.PropertyToID("_BaseColor");

        /// <summary>候補リスト上のインデックス。確定時にそのまま UseCase へ渡す</summary>
        public int Index { get; private set; }

        /// <summary>掴んでいないときに戻る定位置（ワールド）</summary>
        public Pose HomePose { get; private set; }

        /// <summary>掴み判定に使うコライダー</summary>
        public Collider Collider => _collider;

        /// <summary>所持ポイントで買えるか。買えないカードは掴んで読めるが、確定はさせない</summary>
        public bool IsPurchasable { get; private set; } = true;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>現在の強調表示。購入可否が変わったときに色を塗り直すため覚えておく</summary>
        private CardHighlight _currentHighlight;

        /// <summary>コスト文字の既定色。買えないときに灰色へ落とし、戻すときに使う</summary>
        private Color _defaultCostColor = Color.black;

        /// <summary>
        /// 表示内容と定位置を設定する。
        /// 名前・説明などのローカライズ文言は <see cref="SetText"/> で後から入る（ロケール切替時も差し替わる）
        /// </summary>
        public void Setup(int index, UpgradeMasterData upgrade, Pose homePose)
        {
            Index = index;
            HomePose = homePose;
            transform.SetPositionAndRotation(homePose.position, homePose.rotation);

            if (_costText != null)
            {
                _defaultCostColor = _costText.color;
                _costText.text = $"{upgrade.Cost} P";
            }

            // 実際の購入可否は所持ポイントを見て後から SetPurchasable で入る
            SetPurchasable(true);
            SetHighlight(CardHighlight.None);
        }

        /// <summary>ローカライズ済みの文言（名前・レベル・簡略説明・詳細説明）をそれぞれのテキストへ反映する</summary>
        public void SetText(in UpgradeLocalizedText text)
        {
            SetTextIfAssigned(_nameText, text.Title);
            SetTextIfAssigned(_levelText, text.LevelLabel);
            SetTextIfAssigned(_simpleDescriptionText, text.SimpleDescription);
            SetTextIfAssigned(_descriptionText, text.Description);
        }

        /// <summary>定位置へ向けて補間で戻す</summary>
        public void MoveToHome(float followSpeed)
        {
            MoveTo(HomePose, Vector3.one, followSpeed);
        }

        /// <summary>
        /// 指定の手で持ったときのカードの姿勢。
        /// 持ち手（グリップアンカー）が手の位置・角度にぴったり重なるようにカード全体を配置する。
        /// アンカーのTransformをそのままオフセットとして使うので、インスペクタ上で見ながら持ち方を調整できる
        /// </summary>
        public Pose CalcHoldPose(Pose handPose, HandType handType)
        {
            var anchor = handType == HandType.Left ? _leftHandGripAnchor : _rightHandGripAnchor;

            // アンカー未設定でも読める向きになるよう、表をプレイヤー側へ向けて手の少し先に置く
            if (anchor == null)
            {
                return new Pose(
                    handPose.position + handPose.rotation * FallbackHoldOffset,
                    handPose.rotation * Quaternion.Euler(0f, 180f, 0f));
            }

            var anchorLocalRotation = Quaternion.Inverse(transform.rotation) * anchor.rotation;

            // 現在のスケールが乗ったままのオフセットを使う。
            // 持った直後の拡大中でも、持ち手が手から離れずに追従する
            var anchorOffset = Quaternion.Inverse(transform.rotation) * (anchor.position - transform.position);

            // アンカーが手に重なるよう、アンカーぶんだけ戻した姿勢をカードの姿勢とする
            var rotation = handPose.rotation * Quaternion.Inverse(anchorLocalRotation);

            return new Pose(handPose.position - rotation * anchorOffset, rotation);
        }

        /// <summary>指定の姿勢・スケールへ向けて補間で移動する。フレームレートに依存しないよう指数補間を使う</summary>
        public void MoveTo(Pose target, Vector3 targetScale, float followSpeed)
        {
            var t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);

            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, target.position, t),
                Quaternion.Slerp(transform.rotation, target.rotation, t));
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
        }

        /// <summary>
        /// 所持ポイントで買えるかを反映する。買えないカードは灰色にして、確定できないことを見て分かるようにする
        /// </summary>
        public void SetPurchasable(bool isPurchasable)
        {
            IsPurchasable = isPurchasable;

            if (_costText != null)
            {
                _costText.color = isPurchasable ? _defaultCostColor : _unpurchasableColor;
            }

            // 枠色は購入可否でも変わるため塗り直す
            SetHighlight(_currentHighlight);
        }

        /// <summary>掴み・ホバー状態に応じて枠色を変える</summary>
        public void SetHighlight(CardHighlight highlight)
        {
            _currentHighlight = highlight;

            if (_frameRenderer == null)
            {
                return;
            }

            // 買えないカードは掴んでも狙っても灰色のままにし、確定できる候補と見分けられるようにする
            var color = !IsPurchasable
                ? _unpurchasableColor
                : highlight switch
                {
                    CardHighlight.Held => _heldColor,
                    CardHighlight.Hovered => _hoveredColor,
                    _ => _defaultColor
                };

            // マテリアルを複製しないよう MaterialPropertyBlock で色だけ差し替える
            _propertyBlock ??= new MaterialPropertyBlock();
            _frameRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorPropertyId, color);
            _frameRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>テキストが未割り当て（レイアウト調整中にオブジェクトを外した等）でも落ちないように反映する</summary>
        private static void SetTextIfAssigned(TextMeshPro target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
