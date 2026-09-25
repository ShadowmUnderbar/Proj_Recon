using App.Battle.Interface;
using R3;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ゲームオーバー画面（最小実装）。
    /// ランで獲得したアップグレードをスロット1〜3のいずれかに保存するボタンと、
    /// ビルド選択からやり直すリスタートボタンと、メインメニューシーンへ戻るボタンを表示する。
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
            _onRestart.Dispose();
            _onReturnToMainMenu.Dispose();
        }
    }
}
