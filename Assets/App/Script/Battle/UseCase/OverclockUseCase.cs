using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// オーバークロック。
    /// 回避中に被弾を無効化するたびに秒数を溜め、しきい値を超えたら溜めた秒数だけ敵・敵弾を止める。
    /// 発動中はプレイヤーの視点をその場に留め（本体は動ける）、被弾はダメージだけを溜めて終了時に1回で受ける。
    /// バフ・デバフ・周期効果の時間を止める処理は、各DataStore／UseCaseが <see cref="IOverclockDataStore.IsActive"/> を見て行う。
    /// </summary>
    public class OverclockUseCase : IInitializable, IDisposable
    {
        private readonly IOverclockDataStore _overclockDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly ITracerFreezeState _tracerFreezeState;
        private readonly ISepiaToneDataStore _sepiaToneDataStore;
        private readonly OverclockConfig _overclockConfig;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public OverclockUseCase(
            IOverclockDataStore overclockDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IBulletStoreView bulletStoreView,
            ITracerFreezeState tracerFreezeState,
            ISepiaToneDataStore sepiaToneDataStore,
            OverclockConfig overclockConfig
        )
        {
            _overclockDataStore = overclockDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _playerStateDataStore = playerStateDataStore;
            _playerControlPresenter = playerControlPresenter;
            _bulletStoreView = bulletStoreView;
            _tracerFreezeState = tracerFreezeState;
            _sepiaToneDataStore = sepiaToneDataStore;
            _overclockConfig = overclockConfig;
        }

        public void Initialize()
        {
            _playerDodgeParameterDataStore.OnDamagedDuringDodge
                .Subscribe(_ => OnDamageBlocked())
                .AddTo(_disposable);

            // 初期値（非発動）では何もしない
            _overclockDataStore.IsActive
                .Skip(1)
                .Subscribe(OnActiveChanged)
                .AddTo(_disposable);

            // ウェーブが終わったら停止を持ち越さず、その場で終える
            _waveManagerDataStore.IsWavePause
                .Where(isPause => isPause)
                .Subscribe(_ => _overclockDataStore.ForceEnd())
                .AddTo(_disposable);
        }

        private void OnDamageBlocked()
        {
            // ウェーブ間ポーズ中は獲得しない（被弾処理と同基準）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Overclock, out var upgrade))
            {
                return;
            }

            // 発動中の獲得は DataStore 側で無視する
            _overclockDataStore.AddStock(upgrade.Value1.value);
        }

        private void OnActiveChanged(bool isActive)
        {
            // 敵の停止は FreezeUseCase が他の停止要因と合わせて行う
            _bulletStoreView.SetOverclock(isActive);
            _tracerFreezeState.SetOverclock(isActive);
            _playerControlPresenter.SetCameraPinned(isActive);

            if (isActive)
            {
                _sepiaToneDataStore.Apply(_overclockConfig.SepiaTonePreset);
                return;
            }

            _sepiaToneDataStore.Release(_overclockConfig.SepiaTonePreset);

            // 発動中に溜めたダメージを1回のダメージとして受ける（軽減・バリア・被弾条件バフもここで1回通る）
            var stockedDamage = _overclockDataStore.ConsumeStockedDamage();
            if (stockedDamage <= 0f)
            {
                return;
            }

            _playerStateDataStore.TakeDamage(stockedDamage);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
