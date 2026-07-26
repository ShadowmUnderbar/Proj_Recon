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

        // チョークポイント: 直前に出現した敵を寄せる基準にする半径（m）と、Lv3の同位置スポーン判定
        private const float ChokeClusterRadius = 5f;
        private const int ChokeLevelForceCluster = 3;

        // 直近にスポーンした敵の位置（チョークポイントの寄せ基準）。未スポーンなら null
        private Vector3? _lastSpawnPosition;

        // Lv3で「次の1体も同じ位置に出す」ためのフラグ
        private bool _forceClusterNextSpawn;

        // ビッグマウス: マイナー枠を置き換える先の EnemyCode と、コモンラッシュ置換になる最低レベル
        private const string MinorRushEnemyId = "R-002";
        private const string CommonRushEnemyId = "R-001";
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
                var targetPos = ResolveSpawnPosition(playerPos);

                if (!TryResolveSpawnEnemy(rankType, out var enemy))
                {
                    return;
                }

                _enemyDataStore.AddEnemyData(enemy, new Pose(targetPos, Quaternion.identity));
                _lastSpawnPosition = targetPos;
            }
        }

        /// <summary>
        /// チョークポイントを考慮してスポーン位置を決める。
        /// - Lv3で予約された「次の1体も同位置」なら直前位置をそのまま採用
        /// - それ以外は寄せ確率（所持中の最高レベルの Value1）で直前位置の付近に寄せる
        /// - どちらでもなければ通常のランダム位置
        /// </summary>
        private Vector3 ResolveSpawnPosition(Vector3 playerPos)
        {
            // 直前位置が無い（ラン最初の1体）なら通常スポーン
            if (!_lastSpawnPosition.HasValue)
            {
                return _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);
            }

            // Lv3の予約: 次の1体を直前と同じ位置に出す
            if (_forceClusterNextSpawn)
            {
                _forceClusterNextSpawn = false;
                return _lastSpawnPosition.Value;
            }

            var chokeProbability = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.ChokePoint);
            if (chokeProbability > 0f && UnityEngine.Random.value < chokeProbability)
            {
                // Lv3以上なら、この寄せに続けて次の1体も同位置に出すよう予約
                if (_upgradeEffectSimpleCalculatorDataStore.CalcMaxLevel(UpgradeType.ChokePoint) >=
                    ChokeLevelForceCluster)
                {
                    _forceClusterNextSpawn = true;
                }

                return _enemyRandomSpawnCycleDataStore.GetClusteredSpawnPositionFast(_lastSpawnPosition.Value,
                    ChokeClusterRadius);
            }

            return _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);
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
            var enemyId = level >= BigMouseCommonRushLevel ? CommonRushEnemyId : MinorRushEnemyId;

            if (_enemyDataStore.TryGetEnemyMasterData(enemyId, out enemy))
            {
                return true;
            }

            Debug.LogWarning($"[EnemyRandomSpawnUseCase] ビッグマウスの置換先 EnemyCode '{enemyId}' が見つかりません");
            return false;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}