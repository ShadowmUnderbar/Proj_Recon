using System;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ソシオパス: 敵の撃破をソシオパスへ通知し、レイジを得る（発動中なら倍の時間）。
    /// 撃破は誰によるもの（プレイヤー・味方）でも数える
    /// </summary>
    public class SociopathUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly ISociopathDataStore _sociopathDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SociopathUseCase(
            IEnemyDataStore enemyDataStore,
            ISociopathDataStore sociopathDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _sociopathDataStore = sociopathDataStore;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyDead
                .Subscribe(_ => _sociopathDataStore.NotifyKill())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
