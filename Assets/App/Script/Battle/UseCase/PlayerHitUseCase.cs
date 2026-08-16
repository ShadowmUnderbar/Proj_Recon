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
    /// プレイヤーの被弾（敵の近接・敵弾）を受けてHPを減少させる。
    /// ダメージ量は PlayerStateDataStore.OnDamaged として下流（被弾条件バフ等）へ伝搬する。
    /// </summary>
    public class PlayerHitUseCase : IInitializable, IDisposable
    {
        private readonly IBattlePlayerView _battlePlayerView;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerHitUseCase(
            IBattlePlayerView battlePlayerView,
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore
        )
        {
            _battlePlayerView = battlePlayerView;
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
        }

        public void Initialize()
        {
            _battlePlayerView.OnDamaged
                .Subscribe(OnDamaged)
                .AddTo(_disposable);
        }

        private void OnDamaged(PlayerDamagedData damagedData)
        {
            // ウェーブ間ポーズ中は無敵（敵側と同基準でダメージを通さない）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            // 回避の直線移動中はダメージを無効化する。
            // HPもバリアも減らさない代わりに、無効化した事実をOnDamagedDuringDodgeとして流し、
            // 回避中の被弾をトリガーにした処理（カウンター・演出等）を発火できるようにする
            if (_playerDodgeParameterDataStore.IsDodging.CurrentValue)
            {
                _playerDodgeParameterDataStore.NotifyDamageBlocked(damagedData);
                return;
            }

            _playerStateDataStore.TakeDamage(damagedData.Damage);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
