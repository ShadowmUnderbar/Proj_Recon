using System.Text;
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
        [SerializeField, Tooltip("アップグレード名")]
        private TextMeshPro _nameText;

        [SerializeField, Tooltip("レベル表記")]
        private TextMeshPro _levelText;

        [SerializeField, Tooltip("効果値などの説明")]
        private TextMeshPro _descriptionText;

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

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>表示内容と定位置を設定する</summary>
        public void Setup(int index, UpgradeMasterData upgrade, Pose homePose)
        {
            Index = index;
            HomePose = homePose;
            transform.SetPositionAndRotation(homePose.position, homePose.rotation);

            if (_nameText != null)
            {
                _nameText.text = upgrade.NameKey;
            }

            if (_levelText != null)
            {
                _levelText.text = $"Lv.{upgrade.Level}";
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = BuildDescription(upgrade);
            }

            SetHighlight(CardHighlight.None);
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

        /// <summary>掴み・ホバー状態に応じて枠色を変える</summary>
        public void SetHighlight(CardHighlight highlight)
        {
            if (_frameRenderer == null)
            {
                return;
            }

            var color = highlight switch
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

        /// <summary>
        /// カード裏面に載せる効果の説明。マスターデータには符号の種別しか無いため、
        /// 値が設定されているものだけを符号付きで並べる（本実装までの仮表示）
        /// </summary>
        private static string BuildDescription(UpgradeMasterData upgrade)
        {
            var builder = new StringBuilder();

            AppendValue(builder, upgrade.Value1);
            AppendValue(builder, upgrade.Value2);
            AppendValue(builder, upgrade.Value3);
            AppendValue(builder, upgrade.Value4);
            AppendValue(builder, upgrade.Value5);

            return builder.ToString();
        }

        private static void AppendValue(StringBuilder builder, (float value, ParameterType parameterType) parameter)
        {
            if (parameter.parameterType == ParameterType.None)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            var sign = parameter.parameterType == ParameterType.Negative ? "-" : "+";
            builder.Append(sign).Append(Mathf.Abs(parameter.value).ToString("0.##"));
        }
    }
}
