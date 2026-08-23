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
    /// VRでは候補を3Dカード（<see cref="UpgradeCardBoardView"/>）で提示し、掴んで内容を確認して
    /// トリガーで確定させる。非VR（PC/エディタ）ではカードを使わず、従来どおりCanvasのボタンで選ばせる。
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

        [SerializeField, Tooltip("VRでアップグレード候補を並べる3Dカードのボード")]
        private UpgradeCardBoardView _upgradeCardBoardView;

        private readonly Subject<int> _onUpgradeSelected = new();
        public Observable<int> OnUpgradeSelected => _onUpgradeSelected;

        private readonly Subject<Unit> _onNextWavePressed = new();
        public Observable<Unit> OnNextWavePressed => _onNextWavePressed;

        /// <summary>3Dカードで候補を出しているか（VRかつボードが設定されている場合のみ）</summary>
        private bool _isCardMode;

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
            _isCardMode = DebugConfig.IsVRMode
                          && _upgradeCardBoardView != null
                          && _upgradeCardBoardView.Open(upgrades);

            if (_isCardMode)
            {
                // カードと二重に候補が並ばないよう、Canvasのボタンはすべて隠す
                HideAllUpgradeButtons();
                return;
            }

            // 候補ぶんだけボタンを表示し、余りは非表示（候補ゼロなら全非表示）
            for (var i = 0; i < _upgradeButtons.Length; i++)
            {
                var hasCandidate = i < upgrades.Count;
                _upgradeButtons[i].gameObject.SetActive(hasCandidate);

                if (hasCandidate)
                {
                    _upgradeButtonLabels[i].text = $"{upgrades[i].NameKey}\nLv.{upgrades[i].Level}";
                }
            }
        }

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

        public void Close()
        {
            if (_upgradeCardBoardView != null)
            {
                _upgradeCardBoardView.Close();
            }

            _isCardMode = false;
            _shopRoot.SetActive(false);
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
