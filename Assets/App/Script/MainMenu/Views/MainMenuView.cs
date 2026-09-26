using App.MainMenu.Interface;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.MainMenu.Views
{
    /// <summary>
    /// メインメニュー画面。タイトルとSTARTボタンを持つ最小構成。
    /// START後のアップグレードセット選択は同じプレハブ内の RunStartView（共通View）が担当する。
    /// CanvasはVR向けの遅延追従（VrUiFollowCanvasView）を併用し、
    /// VRのハンドレイ・非VRのマウスのどちらでも押せるようにしている。
    /// </summary>
    public class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField, Tooltip("バトルシーンへ進むボタン")]
        private Button _startButton;

        private readonly Subject<Unit> _onStart = new();
        public Observable<Unit> OnStart => _onStart;

        private void Awake()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(() => _onStart.OnNext(Unit.Default));
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_startButton != null)
            {
                _startButton.interactable = interactable;
            }
        }

        public void SetStartVisible(bool visible)
        {
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            _onStart.Dispose();
        }
    }
}
