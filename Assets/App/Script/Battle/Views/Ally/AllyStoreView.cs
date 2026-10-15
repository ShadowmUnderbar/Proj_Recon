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
        private IBattlePlayerView _playerView;

        private readonly Dictionary<int, AllyViewBase> _allies = new();

        // 敵に狙われる味方（狙われない味方は含めない）
        private readonly List<AllyViewBase> _targetableAllies = new();

        // 並ぶ位置の配り直しの作業用
        private readonly List<AllyViewBase> _formationBuffer = new();

        private readonly Subject<(int allyId, float damage)> _onAllyDamaged = new();
        public Observable<(int allyId, float damage)> OnAllyDamaged => _onAllyDamaged;

        private readonly Subject<int> _onShotRequested = new();
        public Observable<int> OnShotRequested => _onShotRequested;

        private readonly Subject<int> _onAllySpawnFailed = new();
        public Observable<int> OnAllySpawnFailed => _onAllySpawnFailed;

        private bool _isPause;

        // プレイヤーの向いている水平方向。後から出た味方にも渡すため持っておく
        private Vector3 _playerForward;

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> bulletFactory,
            IBattlePlayerView playerView
        )
        {
            _bulletFactory = bulletFactory;
            _playerView = playerView;
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
            view.Init(allyData, _bulletFactory, _playerView?.PlayerTransform);
            view.SetPause(_isPause);
            view.SetPlayerForward(_playerForward);

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

            RefreshFormation(allyData.AllyType);
        }

        /// <summary>
        /// 同じ種類の味方に、出現順の番号と数を配り直す（並ぶ位置を詰めて、重ならないようにする）。
        /// Id は出現ごとに1ずつ減るので、Id の降順が出現順
        /// </summary>
        private void RefreshFormation(AllyType allyType)
        {
            _formationBuffer.Clear();
            foreach (var pair in _allies)
            {
                if (pair.Value != null && pair.Value.AllyType == allyType)
                {
                    _formationBuffer.Add(pair.Value);
                }
            }

            _formationBuffer.Sort((a, b) => b.Id.CompareTo(a.Id));
            for (var i = 0; i < _formationBuffer.Count; i++)
            {
                _formationBuffer[i].SetFormation(i, _formationBuffer.Count);
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
                RefreshFormation(view.AllyType);
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

        public void SetPlayerForward(Vector3 forward)
        {
            _playerForward = forward;

            foreach (var view in _allies.Values)
            {
                if (view != null)
                {
                    view.SetPlayerForward(forward);
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
