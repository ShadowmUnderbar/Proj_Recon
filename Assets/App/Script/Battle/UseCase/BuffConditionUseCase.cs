using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// バフの発動条件の入力（敵へのヒット・HP割合）を購読し、BuffStateDataStoreへ通知する
    /// </summary>
    public class BuffConditionUseCase : IInitializable, IDisposable
    {
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IBattleHitPresenter _battleHitPresenter;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BuffConditionUseCase(
            IBuffStateDataStore buffStateDataStore,
            IBattleHitPresenter battleHitPresenter,
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _buffStateDataStore = buffStateDataStore;
            _battleHitPresenter = battleHitPresenter;
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Initialize()
        {
            _battleHitPresenter.OnHit
                .Subscribe(_ => OnHit())
                .AddTo(_disposable);

            // HP・最大HPのどちらが変化してもHP割合を再計算して通知する
            _playerStateDataStore.Health
                .CombineLatest(_playerStateDataStore.MaxHealth,
                    (health, maxHealth) => maxHealth > 0f ? health / maxHealth : 1f)
                .Subscribe(_buffStateDataStore.SetHealthRatio)
                .AddTo(_disposable);
        }

        private void OnHit()
        {
            // ウェーブ間ポーズ中はダメージが通らないため、ヒット数にも数えない（BattleHitUseCaseと同基準）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _buffStateDataStore.NotifyHit();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
