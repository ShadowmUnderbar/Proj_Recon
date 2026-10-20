using System;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// アンガーマネジメント: 被弾をアンガーマネジメントへ通知し、一定時間後にレイジを得る予約をする。
    /// バリアで防いだ被弾も逆ギレと同じく数える
    /// </summary>
    public class AngerManagementUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IAngerManagementDataStore _angerManagementDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public AngerManagementUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IAngerManagementDataStore angerManagementDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _angerManagementDataStore = angerManagementDataStore;
        }

        public void Initialize()
        {
            _playerStateDataStore.OnDamaged
                .Subscribe(damage => _angerManagementDataStore.NotifyDamaged(damage))
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
