using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemyControlUseCase : ITickable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IEnemyPresenter _enemyPresenter;

        [Inject]
        public EnemyControlUseCase(
            IPlayerDataStore playerDataStore,
            IEnemyPresenter enemyPresenter
        )
        {
            _playerDataStore = playerDataStore;
            _enemyPresenter = enemyPresenter;
        }

        public void Tick()
        {
            _enemyPresenter.SetPlayerPose(_playerDataStore.Pose);
            _enemyPresenter.SetPlayerAimDirection(
                _playerDataStore.LeftAimDirection,
                _playerDataStore.RightAimDirection
            );
        }
    }
}