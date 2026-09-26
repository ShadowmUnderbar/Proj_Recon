using App.MainMenu.Interface;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.MainMenu.Views
{
    /// <summary>
    /// メインメニュー画面。タイトル・STARTボタン・OPTIONボタンを持つメインパネルと、
    /// OPTIONで切り替わるオプションパネルの表示制御を担う。
    /// CanvasはVR向けの遅延追従（VrUiFollowCanvasView）を併用し、
    /// VRのハンドレイ・非VRのマウスのどちらでも押せるようにしている。
    /// </summary>
    public class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField, Tooltip("バトルシーンへ進むボタン")]
        private Button _startButton;

        [SerializeField, Tooltip("オプションパネルを開くボタン")]
        private Button _optionButton;

        [SerializeField, Tooltip("タイトルとボタンを載せたメインパネル。オプション表示中は隠す")]
        private GameObject _mainPanel;

        [SerializeField, Tooltip("オプションパネル。OPTIONボタンで表示し、閉じるボタンで隠す")]
        private GameObject _optionPanel;

        private readonly Subject<Unit> _onStart = new();
        public Observable<Unit> OnStart => _onStart;

        private readonly Subject<Unit> _onOption = new();
        public Observable<Unit> OnOption => _onOption;

        private void Awake()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(() => _onStart.OnNext(Unit.Default));
            }

            if (_optionButton != null)
            {
                _optionButton.onClick.AddListener(() => _onOption.OnNext(Unit.Default));
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_startButton != null)
            {
                _startButton.interactable = interactable;
            }

            if (_optionButton != null)
            {
                _optionButton.interactable = interactable;
            }
        }

        public void ShowMainPanel() => SetPanelsActive(mainPanelActive: true);

        public void ShowOptionPanel() => SetPanelsActive(mainPanelActive: false);

        /// <summary>二つのパネルは常にどちらか一方だけを表示する</summary>
        private void SetPanelsActive(bool mainPanelActive)
        {
            if (_mainPanel != null)
            {
                _mainPanel.SetActive(mainPanelActive);
            }

            if (_optionPanel != null)
            {
                _optionPanel.SetActive(!mainPanelActive);
            }
        }

        private void OnDestroy()
        {
            _onStart.Dispose();
            _onOption.Dispose();
        }
    }
}
