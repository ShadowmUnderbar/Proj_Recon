using App.Battle.Interface;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ラン開始時のセット選択UI（最小実装）。
    /// 保存済みスロット3つ（空は押下不可）と「使わずに開始」ボタンを表示する。
    /// </summary>
    public class RunStartView : MonoBehaviour, IRunStartView
    {
        [SerializeField, Tooltip("UI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("見出しテキスト")]
        private Text _headlineText;

        [SerializeField, Tooltip("スロット選択ボタン（3つ・スロット順）")]
        private Button[] _slotButtons;

        [SerializeField, Tooltip("各スロットボタンのラベル（_slotButtonsと同数・同順）")]
        private Text[] _slotButtonLabels;

        [SerializeField, Tooltip("使わずに開始ボタン")]
        private Button _startWithoutLoadButton;

        private readonly Subject<int> _onSlotSelected = new();
        public Observable<int> OnSlotSelected => _onSlotSelected;

        private readonly Subject<Unit> _onStartWithoutLoad = new();
        public Observable<Unit> OnStartWithoutLoad => _onStartWithoutLoad;

        private void Awake()
        {
            for (var i = 0; i < _slotButtons.Length; i++)
            {
                var index = i;
                _slotButtons[i].onClick.AddListener(() => _onSlotSelected.OnNext(index));
            }

            if (_startWithoutLoadButton != null)
            {
                _startWithoutLoadButton.onClick.AddListener(() => _onStartWithoutLoad.OnNext(Unit.Default));
            }

            // 初期状態は非表示
            Hide();
        }

        public void Show(string headline)
        {
            _root.SetActive(true);

            if (_headlineText != null)
            {
                _headlineText.text = headline;
            }
        }

        public void SetSlot(int index, string label, bool interactable)
        {
            if (index < 0 || index >= _slotButtons.Length)
            {
                return;
            }

            _slotButtonLabels[index].text = label;
            _slotButtons[index].interactable = interactable;
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            _onSlotSelected.Dispose();
            _onStartWithoutLoad.Dispose();
        }
    }
}
