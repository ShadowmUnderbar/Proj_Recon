using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// サモンタレット: マージショットを撃ったとき、その手で狙っていた位置へタレットを召喚する
    /// </summary>
    public class SummonTurretUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly ISummonTurretDataStore _summonTurretDataStore;
        private readonly IAllyDataStore _allyDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SummonTurretUseCase(
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerAimDataStore playerAimDataStore,
            ISummonTurretDataStore summonTurretDataStore,
            IAllyDataStore allyDataStore
        )
        {
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerAimDataStore = playerAimDataStore;
            _summonTurretDataStore = summonTurretDataStore;
            _allyDataStore = allyDataStore;
        }

        public void Initialize()
        {
            _playerBulletParameterDataStore.OnShot
                .Where(x => x.shotType == ShotType.Merge)
                .Subscribe(x => OnMergeShot(x.handType))
                .AddTo(_disposables);
        }

        private void OnMergeShot(HandType handType)
        {
            if (!_summonTurretDataStore.CanSummon())
            {
                return;
            }

            var aimPosition = handType == HandType.Left
                ? _playerAimDataStore.LeftAimPosition
                : _playerAimDataStore.RightAimPosition;

            if (!_allyDataStore.TrySpawn(AllyType.Turret, aimPosition, out var allyData))
            {
                return;
            }

            // 出現の失敗（置ける場所が無い）は出現の通知の中で同期的に返り、データが取り除かれる。
            // 出せなかったときはクールダウンを消費せず、次のマージショットで再挑戦できるようにする
            if (!_allyDataStore.TryGetAllyData(allyData.Id, out _))
            {
                return;
            }

            _summonTurretDataStore.StartCoolDown();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
