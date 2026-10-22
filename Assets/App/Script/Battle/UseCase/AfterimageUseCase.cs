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
    /// 回避の開始地点と、回避中に一定距離進むごとに、プレイヤーの残像を残すよう指示する。
    /// 形状の取得・寿命・描画は <see cref="IAfterimageStoreView"/> 側が持ち、ここは「いつ残すか」だけを決める。
    /// </summary>
    public class AfterimageUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IAfterimagePresenter _afterimagePresenter;
        private readonly AfterimageConfig _config;

        private readonly AfterimageSpawnScheduler _scheduler = new();
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public AfterimageUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IAfterimagePresenter afterimagePresenter,
            AfterimageConfig config
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _afterimagePresenter = afterimagePresenter;
            _config = config;
        }

        public void Initialize()
        {
            _playerDodgeParameterDataStore.IsDodging
                .Subscribe(OnDodgingChanged)
                .AddTo(_disposables);
        }

        private void OnDodgingChanged(bool isDodging)
        {
            if (!isDodging)
            {
                _scheduler.End();
                return;
            }

            // 回避の開始地点にも1体残す
            _scheduler.Begin(_playerStateDataStore.Position.Value);
            Spawn();
        }

        public void Tick()
        {
            if (_scheduler.Advance(_playerStateDataStore.Position.Value, _config.SpawnDistance))
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            // 見た目は残す時点の設定を読む。設定アセットを実行中に変えると、次に残す残像から反映される
            _afterimagePresenter.SpawnPlayerAfterimage(_config.Style);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
