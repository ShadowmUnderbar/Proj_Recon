using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 召喚した味方の出現・被弾・攻撃・停止・消去の流れ。
    /// 味方の攻撃はプレイヤーのノーマルショット（非フォーカス）と同じ弾データで撃ち、
    /// 命中もプレイヤーの弾と同じ経路（BattleHitUseCase）を通すため、強化・バフ・クリティカルがそのまま乗る
    /// </summary>
    public class AllyUseCase : IInitializable, IDisposable
    {
        private readonly IAllyDataStore _allyDataStore;
        private readonly IAllyPresenter _allyPresenter;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly ITimeStopDataStore _timeStopDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public AllyUseCase(
            IAllyDataStore allyDataStore,
            IAllyPresenter allyPresenter,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IFreezeDataStore freezeDataStore,
            ITimeStopDataStore timeStopDataStore,
            IGameStateDataStore gameStateDataStore
        )
        {
            _allyDataStore = allyDataStore;
            _allyPresenter = allyPresenter;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _freezeDataStore = freezeDataStore;
            _timeStopDataStore = timeStopDataStore;
            _gameStateDataStore = gameStateDataStore;
        }

        public void Initialize()
        {
            _allyDataStore.OnAllySpawned
                .Subscribe(_allyPresenter.Spawn)
                .AddTo(_disposables);

            _allyDataStore.OnAllyRemoved
                .Subscribe(_allyPresenter.UnSpawn)
                .AddTo(_disposables);

            _allyPresenter.OnAllySpawnFailed
                .Subscribe(_allyDataStore.Remove)
                .AddTo(_disposables);

            _allyPresenter.OnAllyDamaged
                .Subscribe(x => _allyDataStore.Damage(x.allyId, x.damage))
                .AddTo(_disposables);

            _allyPresenter.OnShotRequested
                .Subscribe(OnShotRequested)
                .AddTo(_disposables);

            // ウェーブの終わり（ショップ・ボス撃破）とリスタートのセット選択でウェーブ間ポーズになる。
            // 味方はウェーブをまたがず、ここで全員消す
            _waveManagerDataStore.IsWavePause
                .Where(isPause => isPause)
                .Subscribe(_ => _allyDataStore.RemoveAll())
                .AddTo(_disposables);

            // プレイヤーが撃てない間（フリーズ・時止め）とラン終了後は味方も止める。
            // オーバークロックはプレイヤーが撃ち続けられるため止めない
            _freezeDataStore.IsFreezing
                .CombineLatest(_timeStopDataStore.IsTimeStopped, _gameStateDataStore.IsGameOver,
                    _gameStateDataStore.IsCleared,
                    (isFreezing, isTimeStopped, isGameOver, isCleared) =>
                        isFreezing || isTimeStopped || isGameOver || isCleared)
                .DistinctUntilChanged()
                .Subscribe(_allyPresenter.SetPause)
                .AddTo(_disposables);
        }

        private void OnShotRequested(int allyId)
        {
            if (!_allyDataStore.TryGetAllyData(allyId, out _))
            {
                return;
            }

            var bulletData = _playerBulletParameterDataStore.GetBulletData(ShotType.Normal, AimFocusType.NotFocus);
            _allyPresenter.Shot(allyId, bulletData);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
