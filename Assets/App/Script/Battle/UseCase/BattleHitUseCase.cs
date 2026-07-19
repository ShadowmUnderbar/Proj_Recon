using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class BattleHitUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBattleHitPresenter _battleHitPresenter;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BattleHitUseCase
        (
            IEnemyDataStore enemyDataStore,
            IBattleHitPresenter battleHitPresenter,
            IEnemyPresenter enemyPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IBuffStateDataStore buffStateDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _battleHitPresenter = battleHitPresenter;
            _enemyPresenter = enemyPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _buffStateDataStore = buffStateDataStore;
        }

        public void Initialize()
        {
            _battleHitPresenter.OnHit
                .Subscribe(OnHit)
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyDead
                .Subscribe(x => OnEnemyDead(x).Forget())
                .AddTo(_disposable);
        }

        private void OnHit(HitData hitData)
        {
            // ウェーブ間ポーズ中は敵を無敵化（ダメージを通さない）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            // 貫通ヒット数に応じたダメージ倍率（PenetrationCount条件バフ）を適用する
            hitData.Damage *= _buffStateDataStore.CalcPenetrationMultiply(hitData.PenetrationIndex);

            _enemyDataStore.Damage(hitData);
        }

        private async UniTask OnEnemyDead(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            await _enemyPresenter.Dead(enemyId);

            _enemyDataStore.RemoveEnemyData(enemyId);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}