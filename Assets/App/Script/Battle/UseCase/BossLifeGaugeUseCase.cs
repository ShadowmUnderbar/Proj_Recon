using System;
using System.Collections.Generic;
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
    /// ボス（Boss ランクの敵）の足元に体力ゲージを出し、ボスの位置に追従させて体力の割合を反映する。
    /// 体力を共有するボスは、どれに当たっても全員のゲージが同じ割合で減る。
    /// </summary>
    public class BossLifeGaugeUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IBossLifeGaugePresenter _bossLifeGaugePresenter;

        // ゲージを出している敵（ボスだけ）
        private readonly HashSet<int> _gaugeEnemyIds = new();

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BossLifeGaugeUseCase(
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IBossLifeGaugePresenter bossLifeGaugePresenter
        )
        {
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _bossLifeGaugePresenter = bossLifeGaugePresenter;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyAdded
                .Subscribe(OnEnemyAdded)
                .AddTo(_disposable);

            // 体力を共有する仲間のゲージも同時に減らすため、ボスへの命中ではゲージ全部を更新する（ボスは数体なので安い）
            _enemyDataStore.OnEnemyDamaged
                .Where(_gaugeEnemyIds.Contains)
                .Subscribe(_ => RefreshAllRatios())
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyRemoved
                .Subscribe(OnEnemyRemoved)
                .AddTo(_disposable);

            _enemyPresenter.OnEnemyPoseUpdate
                .Where(x => _gaugeEnemyIds.Contains(x.id))
                .Subscribe(x => _bossLifeGaugePresenter.SetPosition(x.id, x.pose.position))
                .AddTo(_disposable);
        }

        private void OnEnemyAdded(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData)
                || !_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var masterData)
                || masterData.EnemyRankType != EnemyRankType.Boss)
            {
                return;
            }

            _gaugeEnemyIds.Add(enemyId);
            _bossLifeGaugePresenter.Add(enemyId);
            _bossLifeGaugePresenter.SetPosition(enemyId, enemyData.Pose.position);
            _bossLifeGaugePresenter.SetHealthRatio(enemyId, ToRatio(enemyData.Hp, enemyData.MaxHp));
        }

        private void OnEnemyRemoved(int enemyId)
        {
            if (!_gaugeEnemyIds.Remove(enemyId))
            {
                return;
            }

            _bossLifeGaugePresenter.Remove(enemyId);
        }

        private void RefreshAllRatios()
        {
            foreach (var enemyId in _gaugeEnemyIds)
            {
                if (_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
                {
                    _bossLifeGaugePresenter.SetHealthRatio(enemyId, ToRatio(enemyData.Hp, enemyData.MaxHp));
                }
            }
        }

        private static float ToRatio(float current, float max)
        {
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
