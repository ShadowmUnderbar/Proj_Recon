using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    public class BossGroupDataStore : IBossGroupDataStore, IRunResettable
    {
        private readonly IEnemyDataStore _enemyDataStore;

        private readonly List<BossPatternRunner> _groups = new();
        private readonly System.Random _random = new();
        private readonly Dictionary<int, BossPatternRunner> _groupByMemberId = new();

        // 発狂フェイズの切り替えを待っているグループ → 設定（切り替えたら外す）
        private readonly Dictionary<BossPatternRunner, BossGroupConfig> _pendingRage = new();

        [Inject]
        public BossGroupDataStore(IEnemyDataStore enemyDataStore)
        {
            _enemyDataStore = enemyDataStore;
        }

        public bool HasAliveGroup
        {
            get
            {
                // 全滅したグループは次の Tick まで一覧に残るため、終了済みかを個別に見る
                foreach (var runner in _groups)
                {
                    if (!runner.IsFinished)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public IReadOnlyList<int> SpawnGroup(BossGroupConfig config, Pose origin)
        {
            if (config == null || config.Members.Count == 0)
            {
                Debug.LogError($"[{nameof(BossGroupDataStore)}] メンバーのいないボスグループは出現させられません");
                return Array.Empty<int>();
            }

            // 途中まで出現させてから失敗しないよう、先に全メンバーを確認する
            foreach (var member in config.Members)
            {
                if (member.EnemyMasterData == null)
                {
                    Debug.LogError($"[{nameof(BossGroupDataStore)}] {config.name}: EnemyMasterData が未設定のメンバーがあります");
                    return Array.Empty<int>();
                }
            }

            var memberIds = new List<int>(config.Members.Count);
            foreach (var member in config.Members)
            {
                var pose = new Pose(origin.position + origin.rotation * member.SpawnOffset, origin.rotation);
                var enemyData = _enemyDataStore.AddEnemyData(member.EnemyMasterData, pose);
                memberIds.Add(enemyData.Id);
            }

            if (config.SharedHealth)
            {
                _enemyDataStore.LinkSharedHealth(memberIds);
            }

            var runner = new BossPatternRunner(memberIds, config.Pattern, _random);
            _groups.Add(runner);
            if (config.HasRagePhase)
            {
                _pendingRage.Add(runner, config);
            }
            foreach (var id in memberIds)
            {
                _groupByMemberId[id] = runner;
            }

            return memberIds;
        }

        public void UpdateMemberStatus(int enemyId, BossMemberStatus status)
        {
            if (_groupByMemberId.TryGetValue(enemyId, out var runner))
            {
                runner.TrySetStatus(enemyId, status);
            }
        }

        public void RemoveMember(int enemyId)
        {
            if (!_groupByMemberId.Remove(enemyId, out var runner))
            {
                return;
            }

            runner.TryMarkGone(enemyId);
        }

        public void Tick(float deltaTime, List<BossDirectorCommand> output)
        {
            for (var i = _groups.Count - 1; i >= 0; i--)
            {
                var runner = _groups[i];
                if (runner.IsFinished)
                {
                    _groups.RemoveAt(i);
                    _pendingRage.Remove(runner);
                    continue;
                }

                TrySwitchToRage(runner, output);
                runner.Tick(deltaTime, output);
            }
        }

        /// <summary>体力が設定の割合以下になったグループの台本を発狂フェイズ用へ切り替える</summary>
        private void TrySwitchToRage(BossPatternRunner runner, List<BossDirectorCommand> output)
        {
            if (!_pendingRage.TryGetValue(runner, out var config))
            {
                return;
            }

            // 体力を共有するグループは全員同じ値。共有しないグループは生き残りの先頭で判断する
            foreach (var id in runner.MemberIds)
            {
                if (!_enemyDataStore.TryGetEnemyData(id, out var enemyData) || enemyData.IsDead || enemyData.MaxHp <= 0f)
                {
                    continue;
                }

                if (enemyData.Hp / enemyData.MaxHp > config.RageHealthRatio)
                {
                    return;
                }

                break;
            }

            _pendingRage.Remove(runner);
            runner.SwitchPattern(config.RagePattern, output);
        }

        public void ResetRun()
        {
            _groups.Clear();
            _groupByMemberId.Clear();
            _pendingRage.Clear();
        }
    }
}
