using App.Common.Data;
using App.MainMenu.Interface;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.MainMenu.Views
{
    /// <summary>
    /// 部屋に固定設置したオプションパネル。利き手とVRの移動設定を変更する。
    /// 選択中の項目はボタンを押せない状態にして、いま選ばれているものが分かるようにしている。
    /// 操作可否の制御（近づいたときだけ押せる）は<see cref="MenuPanelProximityView"/>が担当する。
    /// </summary>
    public class OptionPanelView : MonoBehaviour, IOptionPanelView
    {
        [SerializeField, Tooltip("利き手を左にするボタン")]
        private Button _dominantHandLeftButton;

        [SerializeField, Tooltip("利き手を右にするボタン")]
        private Button _dominantHandRightButton;

        [SerializeField, Tooltip("移動方式をスムーズ移動にするボタン")]
        private Button _smoothLocomotionButton;

        [SerializeField, Tooltip("移動方式をテレポートにするボタン")]
        private Button _teleportLocomotionButton;

        [SerializeField, Tooltip("移動速度のスライダー")]
        private Slider _moveSpeedSlider;

        [SerializeField, Tooltip("移動速度の数値表示")]
        private TMP_Text _moveSpeedLabel;

        [SerializeField, Tooltip("スナップターン角度のスライダー")]
        private Slider _snapTurnAngleSlider;

        [SerializeField, Tooltip("スナップターン角度の数値表示")]
        private TMP_Text _snapTurnAngleLabel;

        private readonly Subject<HandType> _onDominantHandChanged = new();
        private readonly Subject<LocomotionType> _onLocomotionChanged = new();
        private readonly Subject<float> _onMoveSpeedChanged = new();
        private readonly Subject<int> _onSnapTurnAngleChanged = new();

        public Observable<HandType> OnDominantHandChanged => _onDominantHandChanged;
        public Observable<LocomotionType> OnLocomotionChanged => _onLocomotionChanged;
        public Observable<float> OnMoveSpeedChanged => _onMoveSpeedChanged;
        public Observable<int> OnSnapTurnAngleChanged => _onSnapTurnAngleChanged;

        private void Awake()
        {
            if (_dominantHandLeftButton != null)
            {
                _dominantHandLeftButton.onClick.AddListener(() => _onDominantHandChanged.OnNext(HandType.Left));
            }

            if (_dominantHandRightButton != null)
            {
                _dominantHandRightButton.onClick.AddListener(() => _onDominantHandChanged.OnNext(HandType.Right));
            }

            if (_smoothLocomotionButton != null)
            {
                _smoothLocomotionButton.onClick.AddListener(() => _onLocomotionChanged.OnNext(LocomotionType.Smooth));
            }

            if (_teleportLocomotionButton != null)
            {
                _teleportLocomotionButton.onClick.AddListener(
                    () => _onLocomotionChanged.OnNext(LocomotionType.Teleport));
            }

            ConfigureSliders();

            if (_moveSpeedSlider != null)
            {
                _moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedSliderChanged);
            }

            if (_snapTurnAngleSlider != null)
            {
                _snapTurnAngleSlider.onValueChanged.AddListener(OnSnapTurnAngleSliderChanged);
            }
        }

        /// <summary>
        /// スライダーの範囲を設定値の定義から組み立てる。
        /// 値の反映（<see cref="SetMoveSpeed"/>など）はAwakeの順番に依らず飛んでくるため、
        /// 値を入れる直前にも呼んで、既定の0〜1へクランプされてしまうのを防ぐ。
        /// </summary>
        private void ConfigureSliders()
        {
            if (_moveSpeedSlider != null)
            {
                _moveSpeedSlider.minValue = PlayerSettingRange.MinMoveSpeed;
                _moveSpeedSlider.maxValue = PlayerSettingRange.MaxMoveSpeed;
                _moveSpeedSlider.wholeNumbers = false;
            }

            if (_snapTurnAngleSlider != null)
            {
                _snapTurnAngleSlider.minValue = PlayerSettingRange.MinSnapTurnAngle;
                _snapTurnAngleSlider.maxValue = PlayerSettingRange.MaxSnapTurnAngle;
                _snapTurnAngleSlider.wholeNumbers = true;
            }
        }

        private void OnMoveSpeedSliderChanged(float value)
        {
            // 数値表示は保存の往復を待たずに更新する。ドラッグ中に表示だけ遅れて見えるのを防ぐ
            SetMoveSpeedLabel(value);
            _onMoveSpeedChanged.OnNext(value);
        }

        private void OnSnapTurnAngleSliderChanged(float value)
        {
            // 設定側は刻み幅へ丸めるため、ツマミも同じ位置へ吸着させて表示と実効値をそろえる
            var stepped = Mathf.RoundToInt(value / PlayerSettingRange.SnapTurnAngleStep) *
                          PlayerSettingRange.SnapTurnAngleStep;

            _snapTurnAngleSlider.SetValueWithoutNotify(stepped);
            SetSnapTurnAngleLabel(stepped);
            _onSnapTurnAngleChanged.OnNext(stepped);
        }

        public void SetDominantHand(HandType hand)
        {
            SetSelected(_dominantHandLeftButton, hand == HandType.Left);
            SetSelected(_dominantHandRightButton, hand == HandType.Right);
        }

        public void SetLocomotion(LocomotionType locomotion)
        {
            SetSelected(_smoothLocomotionButton, locomotion == LocomotionType.Smooth);
            SetSelected(_teleportLocomotionButton, locomotion == LocomotionType.Teleport);
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            ConfigureSliders();

            // 通知つきで書き戻すと操作→保存→表示更新が循環するため、通知なしで反映する
            if (_moveSpeedSlider != null)
            {
                _moveSpeedSlider.SetValueWithoutNotify(moveSpeed);
            }

            SetMoveSpeedLabel(moveSpeed);
        }

        public void SetSnapTurnAngle(int angle)
        {
            ConfigureSliders();

            if (_snapTurnAngleSlider != null)
            {
                _snapTurnAngleSlider.SetValueWithoutNotify(angle);
            }

            SetSnapTurnAngleLabel(angle);
        }

        private void SetMoveSpeedLabel(float moveSpeed)
        {
            if (_moveSpeedLabel != null)
            {
                _moveSpeedLabel.text = $"{moveSpeed:F1} m/s";
            }
        }

        private void SetSnapTurnAngleLabel(float angle)
        {
            if (_snapTurnAngleLabel != null)
            {
                _snapTurnAngleLabel.text = $"{angle:F0}°";
            }
        }

        /// <summary>選択中の項目は押せない状態にして、見た目でも選択が分かるようにする</summary>
        private static void SetSelected(Button button, bool isSelected)
        {
            if (button != null)
            {
                button.interactable = !isSelected;
            }
        }

        private void OnDestroy()
        {
            _onDominantHandChanged.Dispose();
            _onLocomotionChanged.Dispose();
            _onMoveSpeedChanged.Dispose();
            _onSnapTurnAngleChanged.Dispose();
        }
    }
}
