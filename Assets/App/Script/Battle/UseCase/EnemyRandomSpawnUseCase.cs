using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemyRandomSpawnUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyRandomSpawnCycleDataStore _enemyRandomSpawnCycleDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        private readonly CompositeDisposable _disposables = new();

        // 陽動: 撃破しきい値（Lv1=3体、Lv2以上=2体）
        private const int DiversionKillThresholdLv1 = 3;
        private const int DiversionKillThresholdHigh = 2;

        // 陽動: 撃破方向の累積とアーム状態
        private int _diversionKillCount;
        private Vector3 _diversionKillDirectionSum;
        private bool _isDiversionArmed;
        private Vector3 _armedDiversionDirection;

        [Inject]
        public EnemyRandomSpawnUseCase(
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _playerStateDataStore = playerStateDataStore;
            _enemyDataStore = enemyDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public void Initialize()
        {
            _enemyRandomSpawnCycleDataStore.OnSpawnCommonEnemy
                .Subscribe(x => SpawnRandomEnemy(x, EnemyRankType.Common))
                .AddTo(_disposables);

            _enemyRandomSpawnCycleDataStore.OnSpawnMinorEnemy
                .Subscribe(x => SpawnRandomEnemy(x, EnemyRankType.Minor))
                .AddTo(_disposables);

            // 陽動: 撃破ごとに方向を蓄積し、しきい値に達したら次のスポーンをアームする
            _enemyDataStore.OnEnemyDead
                .Subscribe(OnEnemyKilled)
                .AddTo(_disposables);
        }

        private void SpawnRandomEnemy(int enemyCount, EnemyRankType rankType)
        {
            var playerPos = _playerStateDataStore.Position.Value;
            for (var i = 0; i < enemyCount; i++)
            {
                var targetPos = ResolveSpawnPosition(playerPos);

                if (!_enemyDataStore.TryGetRandomEnemyMasterData(rankType, _playerStateDataStore.UnlockCoreSkillType, out var enemy))
                {
                    return;
                }

                _enemyDataStore.AddEnemyData(enemy, new Pose(targetPos, Quaternion.identity));
            }
        }

        /// <summary>
        /// スポーン位置を決める。陽動でアーム済みなら「次の1体」として確率判定し、
        /// 当たれば蓄積方向から出現させる。アームは判定の当否に関わらず1回で消費する。
        /// </summary>
        private Vector3 ResolveSpawnPosition(Vector3 playerPos)
        {
            if (!_isDiversionArmed)
            {
                return _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);
            }

            // アームは次の1体で消費（当否に関わらず解除）
            _isDiversionArmed = false;

            var probability = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.Diversion);
            if (probability > 0f && UnityEngine.Random.value < probability)
            {
                return _enemyRandomSpawnCycleDataStore.GetDirectionalSpawnPositionFast(playerPos, _armedDiversionDirection);
            }

            return _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);
        }

        /// <summary>
        /// 陽動: 撃破された敵のプレイヤーから見た方向を蓄積し、しきい値に達したら
        /// その平均方向で次のスポーンをアームする。陽動未所持なら何もしない。
        /// </summary>
        private void OnEnemyKilled(int enemyId)
        {
            var level = _upgradeEffectSimpleCalculatorDataStore.CalcMaxLevel(UpgradeType.Diversion);
            if (level <= 0)
            {
                return;
            }

            // OnEnemyDead時点では敵データはまだ存在する（除去は死亡演出後）
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            var direction = enemyData.Pose.position - _playerStateDataStore.Position.Value;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                _diversionKillDirectionSum += direction.normalized;
            }

            _diversionKillCount++;

            var threshold = level == 1 ? DiversionKillThresholdLv1 : DiversionKillThresholdHigh;
            if (_diversionKillCount < threshold)
            {
                return;
            }

            // しきい値到達: 平均方向でアーム。合計がほぼゼロ（方向が打ち消し合った）なら見送る
            if (_diversionKillDirectionSum.sqrMagnitude > 0.0001f)
            {
                _armedDiversionDirection = _diversionKillDirectionSum.normalized;
                _isDiversionArmed = true;
            }

            _diversionKillCount = 0;
            _diversionKillDirectionSum = Vector3.zero;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
