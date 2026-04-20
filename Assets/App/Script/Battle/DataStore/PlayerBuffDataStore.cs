using System;
using System.Collections.Generic;
using System.Linq;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerBuffDataStore : IPlayerBuffDataStore, ITickable, IDisposable
    {
        private readonly BuffDatabase _buffDatabase;
        private readonly IReadOnlyList<IBuffEffectCalculator> _calculators;

        // KeyはBuffId（同一BuffTypeで異なるIdのバフを並立させる拡張に備える）
        private readonly Dictionary<string, ActiveBuffData> _activeBuffs = new();

        private readonly Subject<BuffType> _onBuffChanged = new();
        public Observable<BuffType> OnBuffChanged => _onBuffChanged;

        public IReadOnlyList<ActiveBuffData> ActiveBuffs => _activeBuffs.Values.ToList();

        [Inject]
        public PlayerBuffDataStore(
            BuffDatabase buffDatabase,
            IEnumerable<IBuffEffectCalculator> calculators)
        {
            _buffDatabase = buffDatabase;
            _calculators = calculators.ToList();
        }

        public void Tick()
        {
            var expiredIds = new List<string>();
            foreach (var (id, buff) in _activeBuffs)
            {
                var remaining = buff.Tick(Time.deltaTime);
                if (remaining <= 0)
                    expiredIds.Add(id);
            }
            foreach (var id in expiredIds)
            {
                var buffType = _activeBuffs[id].BuffType;
                _activeBuffs.Remove(id);
                _onBuffChanged.OnNext(buffType);
            }
        }

        public void AddBuff(BuffMasterData masterData, int stackCount = 1)
        {
            if (stackCount <= 0) return;

            if (_activeBuffs.TryGetValue(masterData.Id, out var existing))
            {
                // MaxStackを超えないよう実際に加算するスタック数を制限
                var addCount = Mathf.Min(stackCount, masterData.MaxStack - existing.StackCount);
                if (addCount <= 0) return;
                existing.AddStack(addCount, masterData.Duration, masterData.HasDuration);
                _onBuffChanged.OnNext(masterData.BuffType);
                return;
            }

            var initialCount = Mathf.Min(stackCount, masterData.MaxStack);
            _activeBuffs[masterData.Id] = new ActiveBuffData(
                masterData.Id,
                masterData.BuffType,
                masterData.Duration,
                masterData.HasDuration,
                initialCount);
            _onBuffChanged.OnNext(masterData.BuffType);
        }

        public bool RemoveBuff(string buffId)
        {
            if (!_activeBuffs.TryGetValue(buffId, out var buff)) return false;
            var buffType = buff.BuffType;
            _activeBuffs.Remove(buffId);
            _onBuffChanged.OnNext(buffType);
            return true;
        }

        public void ClearAllBuffs()
        {
            var types = _activeBuffs.Values.Select(b => b.BuffType).Distinct().ToList();
            _activeBuffs.Clear();
            foreach (var t in types)
                _onBuffChanged.OnNext(t);
        }

        public float GetEffectMultiplier(BuffType buffType)
        {
            var calculator = _calculators.FirstOrDefault(c => c.TargetBuffType == buffType);
            if (calculator == null) return 1f;
            return calculator.Calculate(ActiveBuffs, ResolveMasterData);
        }

        private BuffMasterData ResolveMasterData(string buffId)
        {
            _buffDatabase.TryGetBuffMasterData(buffId, out var data);
            return data;
        }

        public void Dispose()
        {
            _onBuffChanged.Dispose();
        }
    }
}
