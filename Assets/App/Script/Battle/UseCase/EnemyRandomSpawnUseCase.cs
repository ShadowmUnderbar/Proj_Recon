using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.MasterData;
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

        // ビッグマウス: マイナー枠を置き換える先の EnemyCode と、コモンラッシュ置換になる最低レベル
        private const string MinorRushEnemyCode = "MinorRush";
        private const string CommonRushEnemyCode = "CommonRush";
        private const int BigMouseCommonRushLevel = 3;

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
        }

        private void SpawnRandomEnemy(int enemyCount, EnemyRankType rankType)
        {
            var playerPos = _playerStateDataStore.Position.Value;
            for (var i = 0; i < enemyCount; i++)
            {
                var targetPos = _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);

                if (!TryResolveSpawnEnemy(rankType, out var enemy))
                {
                    return;
                }

                _enemyDataStore.AddEnemyData(enemy, new Pose(targetPos, Quaternion.identity));
            }
        }

        /// <summary>
        /// スポーンする敵マスターデータを決める。マイナー枠はビッグマウスによる置換を先に判定し、
        /// 置換が発生しなければ通常のランク別ランダム抽選にフォールバックする。
        /// </summary>
        private bool TryResolveSpawnEnemy(EnemyRankType rankType, out EnemyMasterData enemy)
        {
            if (rankType == EnemyRankType.Minor && TryGetBigMouseOverride(out enemy))
            {
                return true;
            }

            return _enemyDataStore.TryGetRandomEnemyMasterData(
                rankType, _playerStateDataStore.UnlockCoreSkillType, out enemy);
        }

        /// <summary>
        /// ビッグマウス: マイナー枠のスポーン時、Value1 の確率でラッシュ系に置き換える。
        /// Lv1/2 は MinorRush（最も対処が容易なマイナー）、Lv3 は CommonRush（雑魚）に置換する。
        /// 置換しない場合は false を返し、通常抽選に委ねる。
        /// </summary>
        private bool TryGetBigMouseOverride(out EnemyMasterData enemy)
        {
            enemy = null;

            var probability = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.BigMouse);
            if (probability <= 0f || UnityEngine.Random.value >= probability)
            {
                return false;
            }

            var level = _upgradeEffectSimpleCalculatorDataStore.CalcMaxLevel(UpgradeType.BigMouse);
            var enemyCode = level >= BigMouseCommonRushLevel ? CommonRushEnemyCode : MinorRushEnemyCode;

            if (!_enemyDataStore.TryGetEnemyMasterData(enemyCode, out enemy))
            {
                Debug.LogWarning($"[EnemyRandomSpawnUseCase] ビッグマウスの置換先 EnemyCode '{enemyCode}' が見つかりません");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
