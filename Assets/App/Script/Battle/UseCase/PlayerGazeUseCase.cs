using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 視界中央（カメラ正面）に捉えている敵を毎フレーム判定し、注視系アップグレードの効果を反映する。
    /// スネークアイズ（減速）・メデューサ（スタン）・ガン飛ばし（被ダメージ増加）が対象。
    /// 視線から敵ごとの判定球までの距離は EnemyStoreView が数体ずつ順番に更新しており、
    /// ここではその距離をアップグレードごとの半径で絞り込むだけにする（物理クエリは使わない）。
    /// </summary>
    public class PlayerGazeUseCase : ITickable, IInitializable, IDisposable
    {
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly ISnakeEyesDataStore _snakeEyesDataStore;
        private readonly IMedusaDataStore _medusaDataStore;
        private readonly IMeanMugDataStore _meanMugDataStore;

        // アップグレードごとの半径で絞り込んだ注視中の敵（呼び出しごとに上書きする）
        private readonly List<int> _gazedEnemyIds = new();

        // メデューサのスタン対象（ランク条件を満たした注視中の敵）
        private readonly List<int> _stunTargetEnemyIds = new();

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerGazeUseCase(
            IEnemyPresenter enemyPresenter,
            IEnemyDataStore enemyDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            ISnakeEyesDataStore snakeEyesDataStore,
            IMedusaDataStore medusaDataStore,
            IMeanMugDataStore meanMugDataStore
        )
        {
            _enemyPresenter = enemyPresenter;
            _enemyDataStore = enemyDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _snakeEyesDataStore = snakeEyesDataStore;
            _medusaDataStore = medusaDataStore;
            _meanMugDataStore = meanMugDataStore;
        }

        public void Initialize()
        {
            // 消滅した敵の効果状態を破棄する（IDは再利用されないため復活はしない）
            _enemyDataStore.OnEnemyRemoved
                .Subscribe(OnEnemyRemoved)
                .AddTo(_disposables);
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中は敵が停止しているため注視効果も更新しない
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            UpdateSnakeEyes();
            UpdateMedusa();
            UpdateMeanMug();
        }

        /// <summary>
        /// スネークアイズ: 注視中の敵を減速し、範囲外へ出た敵は猶予時間後に元の速度へ戻す。
        /// </summary>
        private void UpdateSnakeEyes()
        {
            if (!_snakeEyesDataStore.TryGetGazeRadius(out var radius))
            {
                return;
            }

            var changes = _snakeEyesDataStore.UpdateGazedEnemies(GetGazedEnemies(radius));
            for (var i = 0; i < changes.Count; i++)
            {
                var (enemyId, speedMultiplier) = changes[i];
                _enemyPresenter.SetSpeedMultiplier(enemyId, speedMultiplier);
            }
        }

        /// <summary>
        /// メデューサ: 注視中のメジャークラス以上の敵をスタンさせ、時間切れで解除する。
        /// </summary>
        private void UpdateMedusa()
        {
            if (!_medusaDataStore.TryGetGazeRadius(out var radius))
            {
                return;
            }

            _stunTargetEnemyIds.Clear();

            var gazedEnemyIds = GetGazedEnemies(radius);
            for (var i = 0; i < gazedEnemyIds.Count; i++)
            {
                var enemyId = gazedEnemyIds[i];

                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
                {
                    continue;
                }

                if (!_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var masterData))
                {
                    continue;
                }

                if (!_medusaDataStore.IsStunTargetRank(masterData.EnemyRankType))
                {
                    continue;
                }

                _stunTargetEnemyIds.Add(enemyId);
            }

            var changes = _medusaDataStore.UpdateStunTargets(_stunTargetEnemyIds);
            for (var i = 0; i < changes.Count; i++)
            {
                var (enemyId, isStun) = changes[i];
                _enemyPresenter.SetStun(enemyId, isStun);
            }
        }

        /// <summary>
        /// ガン飛ばし: 注視中の敵を記録する（被ダメージ倍率は命中時に参照される）。
        /// </summary>
        private void UpdateMeanMug()
        {
            if (!_meanMugDataStore.TryGetGazeRadius(out var radius))
            {
                return;
            }

            _meanMugDataStore.SetGazedEnemies(GetGazedEnemies(radius));
        }

        /// <summary>
        /// 視線からの距離が radius 以内の敵のIDを返す。
        /// 戻り値は呼び出しごとに上書きされる内部リストのため、次の呼び出しまでに使い切ること。
        /// </summary>
        private IReadOnlyList<int> GetGazedEnemies(float radius)
        {
            _gazedEnemyIds.Clear();

            if (radius <= 0f)
            {
                return _gazedEnemyIds;
            }

            var distances = _enemyPresenter.GetGazeEnemyDistances();
            for (var i = 0; i < distances.Count; i++)
            {
                var (enemyId, distanceFromRay) = distances[i];
                if (distanceFromRay <= radius)
                {
                    _gazedEnemyIds.Add(enemyId);
                }
            }

            return _gazedEnemyIds;
        }

        private void OnEnemyRemoved(int enemyId)
        {
            _snakeEyesDataStore.RemoveEnemy(enemyId);
            _medusaDataStore.RemoveEnemy(enemyId);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
