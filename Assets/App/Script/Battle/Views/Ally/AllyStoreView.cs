using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using App.Framework.Utilities;
using R3;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// 召喚した味方の生成・破棄。敵 AI へは「狙える味方のうち最も近いもの」を返す
    /// </summary>
    public class AllyStoreView : MonoBehaviour, IAllyStoreView, IAllyTargetProvider
    {
        [Serializable]
        private class PrefabEntry
        {
            public AllyType AllyType;
            public AllyViewBase Prefab;
        }

        [SerializeField] private PrefabEntry[] _prefabs = Array.Empty<PrefabEntry>();

        [SerializeField, Min(0f), Tooltip("要求された位置からこの距離[m]以内の NavMesh 上に置く。見つからなければ出さない")]
        private float _spawnSampleDistance = 3f;

        private ISimpleObjectFactory<IBulletView> _bulletFactory;

        private readonly Dictionary<int, AllyViewBase> _allies = new();

        // 敵に狙われる味方（狙われない味方は含めない）
        private readonly List<AllyViewBase> _targetableAllies = new();

        private readonly Subject<(int allyId, float damage)> _onAllyDamaged = new();
        public Observable<(int allyId, float damage)> OnAllyDamaged => _onAllyDamaged;

        private readonly Subject<int> _onShotRequested = new();
        public Observable<int> OnShotRequested => _onShotRequested;

        private readonly Subject<int> _onAllySpawnFailed = new();
        public Observable<int> OnAllySpawnFailed => _onAllySpawnFailed;

        private bool _isPause;

        [Inject]
        public void Construct(ISimpleObjectFactory<IBulletView> bulletFactory)
        {
            _bulletFactory = bulletFactory;
        }

        public void Spawn(AllyData allyData)
        {
            var prefab = FindPrefab(allyData.AllyType);
            if (prefab == null)
            {
                FailSpawn(allyData.Id, $"{allyData.AllyType} のプレハブが {nameof(AllyStoreView)} に登録されていません");
                return;
            }

            if (!NavMesh.SamplePosition(allyData.Position, out var hit, _spawnSampleDistance, NavMesh.AllAreas))
            {
                FailSpawn(allyData.Id, $"{allyData.AllyType} を置ける場所が {allyData.Position} の近くにありません");
                return;
            }

            var rotation = allyData.Forward.sqrMagnitude > 0f
                ? Quaternion.LookRotation(allyData.Forward)
                : Quaternion.identity;
            var view = Instantiate(prefab, hit.position, rotation);
            view.Init(allyData, _bulletFactory);
            view.SetPause(_isPause);

            var allyId = allyData.Id;
            view.OnShotRequested
                .Subscribe(_ => _onShotRequested.OnNext(allyId))
                .AddTo(view);
            view.OnDamaged
                .Subscribe(damage => _onAllyDamaged.OnNext((allyId, damage)))
                .AddTo(view);

            _allies.Add(allyId, view);
            if (allyData.IsTargetable)
            {
                _targetableAllies.Add(view);
            }
        }

        /// <summary>
        /// 出現に失敗したことを知らせる。データだけ残ると攻撃も撃破もされない味方になるため、受け取った側で消してもらう
        /// </summary>
        private void FailSpawn(int allyId, string reason)
        {
            Debug.LogWarning($"[{nameof(AllyStoreView)}] {reason}（味方Id: {allyId}）");
            _onAllySpawnFailed.OnNext(allyId);
        }

        private AllyViewBase FindPrefab(AllyType allyType)
        {
            foreach (var entry in _prefabs)
            {
                if (entry != null && entry.AllyType == allyType)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        public void UnSpawn(int allyId)
        {
            if (!_allies.Remove(allyId, out var view))
            {
                return;
            }

            _targetableAllies.Remove(view);

            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        public void Shot(int allyId, BulletData bulletData)
        {
            if (!_allies.TryGetValue(allyId, out var view) || view == null)
            {
                return;
            }

            view.Shot(bulletData);
        }

        public void SetPause(bool isPause)
        {
            _isPause = isPause;

            foreach (var view in _allies.Values)
            {
                if (view != null)
                {
                    view.SetPause(isPause);
                }
            }
        }

        public bool TryGetNearestTarget(Vector3 position, out Transform target, out float sqrDistance)
        {
            target = null;
            sqrDistance = float.MaxValue;

            foreach (var view in _targetableAllies)
            {
                if (view == null)
                {
                    continue;
                }

                var candidate = view.transform;
                var candidateSqrDistance = (candidate.position - position).sqrMagnitude;
                if (candidateSqrDistance >= sqrDistance)
                {
                    continue;
                }

                target = candidate;
                sqrDistance = candidateSqrDistance;
            }

            return target != null;
        }

        private void OnDestroy()
        {
            _onAllyDamaged.Dispose();
            _onShotRequested.Dispose();
            _onAllySpawnFailed.Dispose();
        }
    }
}
