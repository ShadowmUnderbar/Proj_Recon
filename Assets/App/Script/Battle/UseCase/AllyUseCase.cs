using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 召喚した味方の出現・被弾・攻撃・停止・消去の流れ。
    /// 味方の攻撃はプレイヤーのノーマルショット（非フォーカス）と同じ弾データで撃ち、
    /// 命中もプレイヤーの弾と同じ経路（BattleHitUseCase）を通すため、強化・バフ・クリティカルがそのまま乗る
    /// </summary>
    public class AllyUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IAllyDataStore _allyDataStore;
        private readonly IAllyPresenter _allyPresenter;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly ITimeStopDataStore _timeStopDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;

        private readonly CompositeDisposable _disposables = new();

        // 味方へ最後に配った射撃の向き。ほぼ変わらないフレームは配らない
        private Vector3 _lastPlayerForward;

        // 射撃の向きを配り直す最小の角度差[度]
        private const float PlayerForwardResendAngle = 1f;

        [Inject]
        public AllyUseCase(
            IAllyDataStore allyDataStore,
            IAllyPresenter allyPresenter,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerAimDataStore playerAimDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IFreezeDataStore freezeDataStore,
            ITimeStopDataStore timeStopDataStore,
            IGameStateDataStore gameStateDataStore
        )
        {
            _allyDataStore = allyDataStore;
            _allyPresenter = allyPresenter;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerAimDataStore = playerAimDataStore;
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

        public void Tick()
        {
            // 追従する味方がプレイヤーの後ろ側（射線の外）に並べるよう、射撃の向きを渡す。
            // 両手が正反対で向きが決まらないとき（zero）は前の向きのままにする
            var forward = _playerAimDataStore.CenterAimDirection;
            if (forward.sqrMagnitude <= 0f)
            {
                return;
            }

            // まだ一度も配っていない（zero との角度は0になる）ときは必ず配る
            var isFirst = _lastPlayerForward.sqrMagnitude <= 0f;
            if (!isFirst && Vector3.Angle(forward, _lastPlayerForward) < PlayerForwardResendAngle)
            {
                return;
            }

            _lastPlayerForward = forward;
            _allyPresenter.SetPlayerForward(forward);
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
