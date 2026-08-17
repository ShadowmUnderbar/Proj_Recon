using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
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
        // ショップに並べるアップグレードの基本抽選数（目利きで加算される）
        private const int BaseUpgradeChoiceCount = 5;

        // 目利きによる加算後の上限。ShopView.prefab のボタン数（4列×3行のグリッド）と一致させること
        private const int MaxUpgradeChoiceCount = 12;

        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IUpgradeLotteryDataStore _upgradeLotteryDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeSideEffectApplier _upgradeSideEffectApplier;
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IShopPresenter _shopPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;

        private readonly CompositeDisposable _disposable = new();

        private IReadOnlyList<UpgradeMasterData> _currentCandidates;

        [Inject]
        public ShopUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IUpgradeLotteryDataStore upgradeLotteryDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeSideEffectApplier upgradeSideEffectApplier,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IShopPresenter shopPresenter,
            IPlayerControlPresenter playerControlPresenter
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _upgradeLotteryDataStore = upgradeLotteryDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeSideEffectApplier = upgradeSideEffectApplier;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _shopPresenter = shopPresenter;
            _playerControlPresenter = playerControlPresenter;
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
            _currentCandidates = _upgradeLotteryDataStore.DrawUpgrades(GetUpgradeChoiceCount());
            _shopPresenter.Open(_currentCandidates);

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);
        }

        /// <summary>
        /// 今回のショップに並べる候補数。目利きは累積せず最高レベルのみ採用し、View のボタン数を超えないようにする
        /// </summary>
        private int GetUpgradeChoiceCount()
        {
            var extraCount = Mathf.RoundToInt(_upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.Appraisal));
            return Mathf.Min(BaseUpgradeChoiceCount + extraCount, MaxUpgradeChoiceCount);
        }

        private void OnUpgradeSelected(int index)
        {
            if (_currentCandidates == null || index < 0 || index >= _currentCandidates.Count)
            {
                return;
            }

            var selected = _currentCandidates[index];
            _upgradeSessionDataStore.AddUpgrade(selected);

            // GrantBuff/バリア等の付与副作用を適用（読込フローと共通処理）
            _upgradeSideEffectApplier.Apply(selected);

            // 1ウェーブにつき1回だけ選択可能（_currentCandidates=nullで以降の押下を無効化）。
            // 選択したボタンだけを消し、他の候補は「選ばなかったもの」として表示したままにする
            _currentCandidates = null;
            _shopPresenter.HideUpgradeButton(index);
        }

        private void StartNextWave()
        {
            _currentCandidates = null;
            _shopPresenter.Close();
            _playerControlPresenter.SetUiRayEnable(false);

            // ポーズ解除で次ウェーブ再開（時間計測・スポーン・撃破カウントが再始動）
            _waveManagerDataStore.SetWavePause(false);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
