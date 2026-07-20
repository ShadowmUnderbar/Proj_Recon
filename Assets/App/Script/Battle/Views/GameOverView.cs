using App.Battle.Interface;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ゲームオーバー画面（最小実装）。
    /// ランで獲得したアップグレードをスロット1〜3のいずれかに保存するボタンを表示する。
    /// </summary>
    public class GameOverView : MonoBehaviour, IGameOverView
    {
        [SerializeField, Tooltip("ゲームオーバーUI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("見出しテキスト")]
        private Text _headlineText;

        [SerializeField, Tooltip("状態テキスト（保存結果など）")]
        private Text _statusText;

        [SerializeField, Tooltip("スロット保存ボタン（3つ・スロット順）")]
        private Button[] _slotButtons;

        [SerializeField, Tooltip("各スロットボタンのラベル（_slotButtonsと同数・同順）")]
        private Text[] _slotButtonLabels;

        [SerializeField, Tooltip("セーブせずに終了ボタン")]
        private Button _exitButton;

        private readonly Subject<int> _onSaveSlotSelected = new();
        public Observable<int> OnSaveSlotSelected => _onSaveSlotSelected;

        private readonly Subject<Unit> _onExitWithoutSave = new();
        public Observable<Unit> OnExitWithoutSave => _onExitWithoutSave;

        private void Awake()
        {
            for (var i = 0; i < _slotButtons.Length; i++)
            {
                var index = i;
                _slotButtons[i].onClick.AddListener(() => _onSaveSlotSelected.OnNext(index));
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(() => _onExitWithoutSave.OnNext(Unit.Default));
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

            if (_statusText != null)
            {
                _statusText.text = string.Empty;
            }
        }

        public void SetSlotLabel(int index, string label)
        {
            if (index < 0 || index >= _slotButtonLabels.Length)
            {
                return;
            }

            _slotButtonLabels[index].text = label;
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            _onSaveSlotSelected.Dispose();
            _onExitWithoutSave.Dispose();
        }
    }
}
