using System.Collections.Generic;
using App.Battle.Interface;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ウェーブ間に表示するショップUI（仮組み）
    /// アップグレード候補ボタンと「次のウェーブへ」ボタンを表示する
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

        private readonly Subject<int> _onUpgradeSelected = new();
        public Observable<int> OnUpgradeSelected => _onUpgradeSelected;

        private readonly Subject<Unit> _onNextWavePressed = new();
        public Observable<Unit> OnNextWavePressed => _onNextWavePressed;

        private void Awake()
        {
            for (var i = 0; i < _upgradeButtons.Length; i++)
            {
                var index = i;
                _upgradeButtons[i].onClick.AddListener(() => _onUpgradeSelected.OnNext(index));
            }

            _nextWaveButton.onClick.AddListener(() => _onNextWavePressed.OnNext(Unit.Default));

            // 初期状態は非表示
            Close();
        }

        public void Open(IReadOnlyList<UpgradeMasterData> upgrades)
        {
            _shopRoot.SetActive(true);

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
            if (index < 0 || index >= _upgradeButtons.Length)
            {
                return;
            }

            _upgradeButtons[index].gameObject.SetActive(false);
        }

        public void Close()
        {
            _shopRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            _onUpgradeSelected.Dispose();
            _onNextWavePressed.Dispose();
        }
    }
}
