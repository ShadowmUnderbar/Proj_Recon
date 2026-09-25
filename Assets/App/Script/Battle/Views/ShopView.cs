using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ウェーブ間に表示するショップUI（仮組み）
    /// アップグレード候補と「次のウェーブへ」ボタンを表示する。
    ///
    /// 候補は3Dカード（<see cref="UpgradeCardBoardView"/>）で提示する。VRでは掴んで内容を確認して
    /// トリガーで確定させ、非VR（PC/エディタ）ではマウスで狙ってクリックした時点で確定する。
    /// カードを並べられなかったときだけ従来どおりCanvasのボタンで選ばせる。
    /// 「次のウェーブへ」ボタンはどちらの場合もCanvas側をそのまま使う
    /// </summary>
    public class ShopView : MonoBehaviour, IShopView
    {
        [SerializeField, Tooltip("ショップUI全体のルート（開閉で表示切替）")]
        private GameObject _shopRoot;

        [SerializeField, Tooltip("アップグレード候補ボタン（抽選数ぶん配置）")]
        private Button[] _upgradeButtons;

        [SerializeField, Tooltip("各アップグレードボタンのラベル（_upgradeButtonsと同数・同順）")]
        private Text[] _upgradeButtonLabels;

        [SerializeField, Tooltip("次のウェーブへボタン")]
        private Button _nextWaveButton;

        [SerializeField, Tooltip("所持ポイントの表示")]
        private Text _currentPointLabel;

        [SerializeField, Tooltip("アップグレード候補を並べる3Dカードのボード")]
        private UpgradeCardBoardView _upgradeCardBoardView;

        [SerializeField, Tooltip("背景パネル。3Dカードを出すときは手前に被らないよう隠す")]
        private Graphic _panelBackground;

        private readonly Subject<int> _onUpgradeSelected = new();
        public Observable<int> OnUpgradeSelected => _onUpgradeSelected;

        private readonly Subject<Unit> _onNextWavePressed = new();
        public Observable<Unit> OnNextWavePressed => _onNextWavePressed;

        /// <summary>3Dカードで候補を出しているか（VRかつボードが設定されている場合のみ）</summary>
        private bool _isCardMode;

        /// <summary>フォールバックのボタン表示で文言と併記するため、候補ごとの購入コストを覚えておく</summary>
        private readonly List<int> _buttonCosts = new();

        private void Awake()
        {
            for (var i = 0; i < _upgradeButtons.Length; i++)
            {
                var index = i;
                _upgradeButtons[i].onClick.AddListener(() => _onUpgradeSelected.OnNext(index));
            }

            _nextWaveButton.onClick.AddListener(() => _onNextWavePressed.OnNext(Unit.Default));

            if (_upgradeCardBoardView != null)
            {
                _upgradeCardBoardView.OnCardConfirmed
                    .Subscribe(index => _onUpgradeSelected.OnNext(index))
                    .AddTo(this);
            }

            // 初期状態は非表示
            Close();
        }

        public void Open(IReadOnlyList<UpgradeMasterData> upgrades)
        {
            _shopRoot.SetActive(true);

            // カードを並べられなかった場合（カメラ未取得・プレハブ未設定）は候補が選べなくなるため、
            // Canvasのボタンへフォールバックする
            _isCardMode = _upgradeCardBoardView != null
                          && _upgradeCardBoardView.Open(upgrades, !DebugConfig.IsVRMode);

            // 非VRではCanvasがScreenSpaceOverlayで背景パネルが手前に描かれ、カードが一切見えなくなるため隠す。
            // VRのCanvasはカードとは別の位置に置かれるWorldSpaceなので、backdropはそのまま残す
            SetPanelBackgroundVisible(!(_isCardMode && !DebugConfig.IsVRMode));

            if (_isCardMode)
            {
                // カードと二重に候補が並ばないよう、Canvasのボタンはすべて隠す
                HideAllUpgradeButtons();
                return;
            }

            // 候補ぶんだけボタンを表示し、余りは非表示（候補ゼロなら全非表示）
            _buttonCosts.Clear();
            for (var i = 0; i < _upgradeButtons.Length; i++)
            {
                var hasCandidate = i < upgrades.Count;
                _upgradeButtons[i].gameObject.SetActive(hasCandidate);

                if (hasCandidate)
                {
                    // 文言は SetUpgradeText で入るまでキーを仮表示する
                    _buttonCosts.Add(upgrades[i].Cost);
                    _upgradeButtonLabels[i].text = FormatButtonLabel(upgrades[i].NameKey, string.Empty, upgrades[i].Cost);
                    _upgradeButtons[i].interactable = true;
                }
            }
        }

        public void SetUpgradeText(int index, in UpgradeLocalizedText text)
        {
            if (_isCardMode)
            {
                _upgradeCardBoardView.SetText(index, text);
                return;
            }

            if (index < 0 || index >= _upgradeButtonLabels.Length || index >= _buttonCosts.Count)
            {
                return;
            }

            _upgradeButtonLabels[index].text = FormatButtonLabel(text.Title, text.LevelLabel, _buttonCosts[index]);
        }

        // フォールバックのボタンは1つのラベルしか無いため、名前・レベル・購入コストをまとめて出す（コスト0は無償の候補）。
        // レベル表記はローカライズ側で「-レベル1」のように区切り込みで定義されているため、名前の直後に続けて並べる
        private static string FormatButtonLabel(string title, string levelLabel, int cost) =>
            $"{title}{levelLabel}\n{cost} P";

        public void HideUpgradeButton(int index)
        {
            if (_isCardMode)
            {
                _upgradeCardBoardView.RemoveCard(index);
                return;
            }

            if (index < 0 || index >= _upgradeButtons.Length)
            {
                return;
            }

            _upgradeButtons[index].gameObject.SetActive(false);
        }

        /// <summary>掴み操作用の入力をカードボードへ中継する</summary>
        public void UpdateHandInput(in ShopHandInput input)
        {
            if (!_isCardMode)
            {
                return;
            }

            _upgradeCardBoardView.UpdateHandInput(input);
        }

        /// <summary>非VRのポインタ入力をカードボードへ中継する</summary>
        public void UpdatePointerInput(in ShopPointerInput input)
        {
            if (!_isCardMode)
            {
                return;
            }

            _upgradeCardBoardView.UpdatePointerInput(input);
        }

        public void SetCurrentPoint(int currentPoint)
        {
            if (_currentPointLabel == null)
            {
                return;
            }

            _currentPointLabel.text = $"所持ポイント: {currentPoint} P";
        }

        public void SetPurchasable(int index, bool isPurchasable)
        {
            if (_isCardMode)
            {
                _upgradeCardBoardView.SetPurchasable(index, isPurchasable);
                return;
            }

            if (index < 0 || index >= _upgradeButtons.Length)
            {
                return;
            }

            _upgradeButtons[index].interactable = isPurchasable;
        }

        public void Close()
        {
            if (_upgradeCardBoardView != null)
            {
                _upgradeCardBoardView.Close();
            }

            _isCardMode = false;
            SetPanelBackgroundVisible(true);
            _shopRoot.SetActive(false);
        }

        private void SetPanelBackgroundVisible(bool isVisible)
        {
            if (_panelBackground != null)
            {
                _panelBackground.enabled = isVisible;
            }
        }

        private void HideAllUpgradeButtons()
        {
            foreach (var button in _upgradeButtons)
            {
                button.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _onUpgradeSelected.Dispose();
            _onNextWavePressed.Dispose();
        }
    }
}
