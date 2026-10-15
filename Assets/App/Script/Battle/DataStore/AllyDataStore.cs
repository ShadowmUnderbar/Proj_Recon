using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 召喚した味方の実行時データ。種類ごとの体力・攻撃間隔などは <see cref="AllyConfig"/> から決める
    /// </summary>
    public class AllyDataStore : IAllyDataStore, IRunResettable, IDisposable
    {
        // 敵のIdは0以上の乱数、プレイヤーは1、「フォーカス対象なし」は-1 のため、
        // 味方はそれらと重ならない負の値から振る
        private const int FirstAllyId = -100;

        private readonly AllyConfig _allyConfig;

        private readonly Dictionary<int, AllyData> _allies = new();

        // 一括消去で列挙中の辞書を変更しないための作業用
        private readonly List<int> _removeBuffer = new();

        private readonly Subject<AllyData> _onAllySpawned = new();
        public Observable<AllyData> OnAllySpawned => _onAllySpawned;

        private readonly Subject<int> _onAllyRemoved = new();
        public Observable<int> OnAllyRemoved => _onAllyRemoved;

        private int _nextAllyId = FirstAllyId;

        [Inject]
        public AllyDataStore(AllyConfig allyConfig)
        {
            _allyConfig = allyConfig;
        }

        public bool TrySpawn(AllyType allyType, Vector3 position, out AllyData allyData)
        {
            if (_allyConfig == null || !_allyConfig.TryGetEntry(allyType, out var entry))
            {
                Debug.LogWarning($"[{nameof(AllyDataStore)}] {allyType} のパラメータが {nameof(AllyConfig)} に無いため出せません");
                allyData = null;
                return false;
            }

            allyData = new AllyData(_nextAllyId--, allyType, position, entry.MaxHp, entry.AttackInterval,
                entry.AttackRange, entry.IsTargetable);
            _allies.Add(allyData.Id, allyData);
            _onAllySpawned.OnNext(allyData);
            return true;
        }

        public bool TryGetAllyData(int allyId, out AllyData allyData)
        {
            return _allies.TryGetValue(allyId, out allyData);
        }

        public void Damage(int allyId, float damage)
        {
            if (!_allies.TryGetValue(allyId, out var allyData))
            {
                return;
            }

            allyData.Hp = Mathf.Max(0f, allyData.Hp - damage);
            if (allyData.Hp > 0f)
            {
                return;
            }

            Remove(allyId);
        }

        public void Remove(int allyId)
        {
            if (!_allies.Remove(allyId))
            {
                return;
            }

            _onAllyRemoved.OnNext(allyId);
        }

        public void RemoveAll()
        {
            _removeBuffer.Clear();
            _removeBuffer.AddRange(_allies.Keys);

            foreach (var allyId in _removeBuffer)
            {
                Remove(allyId);
            }
        }

        public void ResetRun()
        {
            RemoveAll();
            _nextAllyId = FirstAllyId;
        }

        public void Dispose()
        {
            _onAllySpawned.Dispose();
            _onAllyRemoved.Dispose();
        }
    }
}
