using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemyControlUseCase : ITickable
    {
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyDataStore _enemyDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public EnemyControlUseCase(
            IPlayerAimDataStore playerAimDataStore,
            IEnemyPresenter enemyPresenter,
            IEnemyDataStore enemyDataStore
        )
        {
            _playerAimDataStore = playerAimDataStore;
            _enemyPresenter = enemyPresenter;
            _enemyDataStore = enemyDataStore;

            _enemyPresenter.OnEnemyPoseUpdate.Subscribe(OnEnemyPoseUpdate)
                .AddTo(_disposables);
        }

        private void OnEnemyPoseUpdate((int id, Pose pose) enemyPose)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyPose.id, out _))
            {
                return;
            }

            _enemyDataStore.UpdateEnemyPose(enemyPose.id, enemyPose.pose);
        }

        public void Tick()
        {
            _enemyPresenter.SetPlayerAimDirection(
                _playerAimDataStore.LeftAimDirection,
                _playerAimDataStore.RightAimDirection
            );
        }
    }
}
