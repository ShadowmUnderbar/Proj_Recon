using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 床崩壊の波に合わせて、弾・敵・ポイント粒子を中心に近い順に消す。
    /// 波が届いた（真下の床が落ち始めた）ものから消し、崩壊しきった時点で残っているものもすべて消す。
    /// 敵は撃破扱いにせず取り除く（ポイント・撃破数・撃破時の効果は出ない）ので、次のウェーブは敵がいない状態から始まる。
    /// 崩壊の間、弾と粒子は <see cref="FreezeUseCase"/> がその場に止めている（粒子は回収もされない）
    /// </summary>
    public class FloorCollapseSweepUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IFloorCollapseDataStore _floorCollapseDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly IPointParticlePresenter _pointParticlePresenter;

        private readonly CompositeDisposable _disposables = new();
        private readonly List<int> _enemyIdBuffer = new();
        private readonly Func<Vector3, bool> _isCollapsedAt;

        [Inject]
        public FloorCollapseSweepUseCase(
            IFloorCollapseDataStore floorCollapseDataStore,
            IEnemyDataStore enemyDataStore,
            IBulletStoreView bulletStoreView,
            IPointParticlePresenter pointParticlePresenter
        )
        {
            _floorCollapseDataStore = floorCollapseDataStore;
            _enemyDataStore = enemyDataStore;
            _bulletStoreView = bulletStoreView;
            _pointParticlePresenter = pointParticlePresenter;
            // 毎フレームのデリゲート生成を避けるため1回だけ作る
            _isCollapsedAt = floorCollapseDataStore.IsCollapsedAt;
        }

        public void Initialize()
        {
            // 弾は毎フレーム探し直さず、崩壊の開始時点で残っていたものを順に消す
            _floorCollapseDataStore.OnStarted
                .Subscribe(_ => _bulletStoreView.BeginSweep())
                .AddTo(_disposables);

            // 崩壊しきったら、範囲の外などで波が届かなかったものも含めて残りを消す
            _floorCollapseDataStore.OnCompleted
                .Subscribe(_ => SweepAll())
                .AddTo(_disposables);

            // 崩壊しきる前に打ち切られたときも、ウェーブクリアで消すはずだった弾・粒子を持ち越さない
            // （ウェーブクリアは崩壊の再生中だけ弾・粒子の全消去をこちらへ任せている。敵は従来どおり持ち越す）
            _floorCollapseDataStore.Phase
                .Pairwise()
                .Where(pair => pair.Previous == FloorCollapsePhase.Collapsing && pair.Current == FloorCollapsePhase.Idle)
                .Subscribe(_ =>
                {
                    _bulletStoreView.AllRemove();
                    _pointParticlePresenter.AllRemove();
                })
                .AddTo(_disposables);
        }

        public void Tick()
        {
            if (_floorCollapseDataStore.Phase.CurrentValue != FloorCollapsePhase.Collapsing)
            {
                return;
            }

            SweepEnemies();
            _bulletStoreView.SweepWhere(_isCollapsedAt);
            _pointParticlePresenter.RemoveWhere(_isCollapsedAt);
        }

        private void SweepEnemies()
        {
            // 取り除くと一覧が変わるので、先に対象を集めてから消す
            _enemyIdBuffer.Clear();
            foreach (var enemy in _enemyDataStore.Enemies)
            {
                if (_isCollapsedAt(enemy.Pose.position))
                {
                    _enemyIdBuffer.Add(enemy.Id);
                }
            }

            foreach (var enemyId in _enemyIdBuffer)
            {
                _enemyDataStore.RemoveEnemyData(enemyId);
            }
        }

        private void SweepAll()
        {
            _enemyDataStore.RemoveAllEnemyData();
            _bulletStoreView.AllRemove();
            _pointParticlePresenter.AllRemove();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
