using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ランをやり直すための初期化処理。
    /// ラン限りの状態を持つDataStore（<see cref="IRunResettable"/>）をまとめて初期化し、
    /// 場に残っている敵・弾・ポイント粒子を消してからビルド選択へ戻す。
    /// リセット対象はVContainerへの登録から集めるため、DataStoreを追加するときは
    /// <see cref="IRunResettable"/> を実装するだけでよい。
    /// </summary>
    public class RunResetUseCase
    {
        private readonly IReadOnlyList<IRunResettable> _runResettables;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly IPointParticlePresenter _pointParticlePresenter;
        private readonly IRunStartDataStore _runStartDataStore;

        [Inject]
        public RunResetUseCase(
            IReadOnlyList<IRunResettable> runResettables,
            IBulletStoreView bulletStoreView,
            IPointParticlePresenter pointParticlePresenter,
            IRunStartDataStore runStartDataStore
        )
        {
            _runResettables = runResettables;
            _bulletStoreView = bulletStoreView;
            _pointParticlePresenter = pointParticlePresenter;
            _runStartDataStore = runStartDataStore;
        }

        /// <summary>ラン状態を初期化し、ビルド選択からやり直せる状態にする</summary>
        public void ResetRun()
        {
            for (var i = 0; i < _runResettables.Count; i++)
            {
                _runResettables[i].ResetRun();
            }

            // 弾・粒子の消去はリセットの後に行う。
            // リセットの途中で敵の撃破が誘発されるとポイント粒子が新たに生まれるため、
            // 先に消すと生まれ直したぶんが次のランへ持ち越されてしまう
            // （敵そのものはEnemyDataStoreのリセットからUnSpawnまで伝搬する）
            _bulletStoreView.AllRemove();
            _pointParticlePresenter.AllRemove();

            // ビルド選択を開き直す（UIの表示とゲーム停止はRunStartUseCaseが担当する）
            _runStartDataStore.SetSelecting(true);
        }
    }
}
