using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// フリーズの開始・解除に合わせて、敵と弾をその場で止める。
    /// プレイヤーの移動・回避・射撃は各UseCaseが入口で <see cref="IFreezeDataStore.IsFreezing"/> を見て止める。
    /// </summary>
    public class FreezeUseCase : IInitializable, IDisposable
    {
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IBulletStoreView _bulletStoreView;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public FreezeUseCase(
            IFreezeDataStore freezeDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyPresenter enemyPresenter,
            IBulletStoreView bulletStoreView
        )
        {
            _freezeDataStore = freezeDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _enemyPresenter = enemyPresenter;
            _bulletStoreView = bulletStoreView;
        }

        public void Initialize()
        {
            _freezeDataStore.IsFreezing
                .Subscribe(OnFreezeChanged)
                .AddTo(_disposable);
        }

        private void OnFreezeChanged(bool isFreezing)
        {
            // ウェーブ間ポーズと同じ停止機構を使うため、解除時にポーズ中の停止を解いてしまわないよう論理和で渡す
            _enemyPresenter.SetPause(isFreezing || _waveManagerDataStore.IsWavePause.Value);

            _bulletStoreView.SetPause(isFreezing);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
