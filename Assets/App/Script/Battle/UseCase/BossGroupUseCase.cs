using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ボスグループの台本進行（<see cref="IBossGroupDataStore"/>）と個体をつなぐ。
    /// 個体の状態を DataStore へ渡し、DataStore が出した行動・待機の命令を個体へ届ける。
    /// </summary>
    public class BossGroupUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IBossGroupDataStore _bossGroupDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private readonly List<BossDirectorCommand> _commands = new();
        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BossGroupUseCase(
            IBossGroupDataStore bossGroupDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IFreezeDataStore freezeDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _bossGroupDataStore = bossGroupDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _freezeDataStore = freezeDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Initialize()
        {
            _enemyPresenter.OnBossMemberStatusChanged
                .Subscribe(x => _bossGroupDataStore.UpdateMemberStatus(x.id, x.status))
                .AddTo(_disposable);

            // 撃破演出の完了を待たずに台本の対象から外す
            _enemyDataStore.OnEnemyDead
                .Subscribe(_bossGroupDataStore.RemoveMember)
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyRemoved
                .Subscribe(_bossGroupDataStore.RemoveMember)
                .AddTo(_disposable);
        }

        public void Tick()
        {
            // 敵が止まっている間は台本も止める（待ち時間を進めない・命令も出さない）
            if (_freezeDataStore.IsFreezing.CurrentValue || _waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _bossGroupDataStore.Tick(Time.deltaTime, _commands);

            foreach (var command in _commands)
            {
                Dispatch(command);
            }

            _commands.Clear();
        }

        private void Dispatch(BossDirectorCommand command)
        {
            switch (command.Type)
            {
                case BossDirectorCommandType.Act:
                    _enemyPresenter.CommandBossAction(command.EnemyId, command.ActionIndex);
                    break;
                case BossDirectorCommandType.Hold:
                    _enemyPresenter.SetBossHold(command.EnemyId, true);
                    break;
                case BossDirectorCommandType.Release:
                    _enemyPresenter.SetBossHold(command.EnemyId, false);
                    break;
                default:
                    Debug.LogError($"[{nameof(BossGroupUseCase)}] 未対応の命令です: {command}");
                    break;
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
