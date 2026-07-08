using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ウェーブ間ショップの制御（仮組み）
    /// ウェーブ突破（OnWaveAdvanced）でショップを開き、アップグレード選択を1回受け付け、
    /// 「次のウェーブへ」でショップを閉じてウェーブを再開する
    /// </summary>
    public class ShopUseCase : IInitializable, IDisposable
    {
        // ショップに並べるアップグレードの抽選数
        private const int UpgradeChoiceCount = 5;

        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IUpgradeLotteryDataStore _upgradeLotteryDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly IShopPresenter _shopPresenter;

        private readonly CompositeDisposable _disposable = new();

        private IReadOnlyList<UpgradeMasterData> _currentCandidates;

        [Inject]
        public ShopUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IUpgradeLotteryDataStore upgradeLotteryDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IShopPresenter shopPresenter
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _upgradeLotteryDataStore = upgradeLotteryDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _shopPresenter = shopPresenter;
        }

        public void Initialize()
        {
            // ウェーブ突破時（この時点で IsWavePause=true・敵/弾クリア済み）にショップを開く
            _waveManagerDataStore.OnWaveAdvanced
                .Subscribe(_ => OpenShop())
                .AddTo(_disposable);

            _shopPresenter.OnUpgradeSelected
                .Subscribe(OnUpgradeSelected)
                .AddTo(_disposable);

            _shopPresenter.OnNextWavePressed
                .Subscribe(_ => StartNextWave())
                .AddTo(_disposable);
        }

        private void OpenShop()
        {
            // 出現可能なアップグレードから抽選（候補ゼロならボタンはView側で全非表示になる）
            _currentCandidates = _upgradeLotteryDataStore.DrawUpgrades(UpgradeChoiceCount);
            _shopPresenter.Open(_currentCandidates);
        }

        private void OnUpgradeSelected(int index)
        {
            if (_currentCandidates == null || index < 0 || index >= _currentCandidates.Count)
            {
                return;
            }

            _upgradeSessionDataStore.AddUpgrade(_currentCandidates[index]);

            // 1ウェーブにつき1回だけ選択可能。選択後は候補ボタンを閉じる
            _currentCandidates = null;
            _shopPresenter.HideUpgradeButtons();
        }

        private void StartNextWave()
        {
            _currentCandidates = null;
            _shopPresenter.Close();

            // ポーズ解除で次ウェーブ再開（時間計測・スポーン・撃破カウントが再始動）
            _waveManagerDataStore.SetWavePause(false);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
