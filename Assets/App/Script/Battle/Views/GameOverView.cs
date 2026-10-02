using App.Battle.Interface;
using R3;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ランの結果画面（ゲームオーバー・クリア共用の最小実装）。
    /// ランで獲得したアップグレードをスロット1〜3のいずれかに保存するボタンと、
    /// ビルド選択からやり直すリスタートボタンと、メインメニューシーンへ戻るボタンを表示する。
    /// クリア直後は見出しだけを出し、少し置いてからボタンを出す。
    /// </summary>
    public class GameOverView : MonoBehaviour, IGameOverView
    {
        [SerializeField, Tooltip("ゲームオーバーUI全体のルート（表示切替）")]
        private GameObject _root;

        [SerializeField, Tooltip("見出しテキスト")]
        private Text _headlineText;

        [SerializeField, Tooltip("獲得アップグレードの一覧テキスト（任意）")]
        private Text _upgradeListText;

        [SerializeField, Tooltip("状態テキスト（保存結果など）")]
        private Text _statusText;

        [SerializeField, Tooltip("スロット保存ボタン（3つ・スロット順）")]
        private Button[] _slotButtons;

        [SerializeField, Tooltip("各スロットボタンのラベル（_slotButtonsと同数・同順）")]
        private Text[] _slotButtonLabels;

        [FormerlySerializedAs("_exitButton")]
        [SerializeField, Tooltip("リスタートボタン（押すまでに保存していなければ保存せずやり直す）")]
        private Button _restartButton;

        [SerializeField, Tooltip("メインメニューへ戻るボタン")]
        private Button _returnToMainMenuButton;

        private readonly Subject<int> _onSaveSlotSelected = new();
        public Observable<int> OnSaveSlotSelected => _onSaveSlotSelected;

        private readonly Subject<Unit> _onRestart = new();
        public Observable<Unit> OnRestart => _onRestart;

        private readonly Subject<Unit> _onReturnToMainMenu = new();
        public Observable<Unit> OnReturnToMainMenu => _onReturnToMainMenu;

        private void Awake()
        {
            for (var i = 0; i < _slotButtons.Length; i++)
            {
                var index = i;
                _slotButtons[i].onClick.AddListener(() => _onSaveSlotSelected.OnNext(index));
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(() => _onRestart.OnNext(Unit.Default));
            }

            if (_returnToMainMenuButton != null)
            {
                _returnToMainMenuButton.onClick.AddListener(() => _onReturnToMainMenu.OnNext(Unit.Default));
            }

            // 初期状態は非表示
            Hide();
        }

        public void Show(string headline)
        {
            ShowInternal(headline, true);
        }

        public void ShowHeadlineOnly(string headline)
        {
            ShowInternal(headline, false);
        }

        private void ShowInternal(string headline, bool isButtonVisible)
        {
            _root.SetActive(true);

            if (_headlineText != null)
            {
                _headlineText.text = headline;
            }

            if (_statusText != null)
            {
                _statusText.text = string.Empty;
                _statusText.gameObject.SetActive(isButtonVisible);
            }

            if (_upgradeListText != null)
            {
                _upgradeListText.text = string.Empty;
                _upgradeListText.gameObject.SetActive(isButtonVisible);
            }

            SetButtonsVisible(isButtonVisible);
        }

        public void SetUpgradeList(string list)
        {
            if (_upgradeListText != null)
            {
                _upgradeListText.text = list;
            }
        }

        private void SetButtonsVisible(bool isVisible)
        {
            foreach (var button in _slotButtons)
            {
                button.gameObject.SetActive(isVisible);
            }

            if (_restartButton != null)
            {
                _restartButton.gameObject.SetActive(isVisible);
            }

            if (_returnToMainMenuButton != null)
            {
                _returnToMainMenuButton.gameObject.SetActive(isVisible);
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
            _onRestart.Dispose();
            _onReturnToMainMenu.Dispose();
        }
    }
}
