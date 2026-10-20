using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ウイルスの時間を進め、1秒ごとのダメージを敵へ与える。
    /// ウェーブ間ポーズ中・オーバークロック中・ゲームオーバー中は時間もダメージも止める。
    /// ダメージは固定ダメージ（方向倍率・最低保証なし）で、射撃フォームは渡さない（フォーム条件のバフを駆動させない）。
    /// 敵が消えたらウイルスも取り除く（撃破演出中は残すので、死亡時にウイルスを参照できる）
    /// </summary>
    public class VirusUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IVirusDataStore _virusDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IOverclockDataStore _overclockDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;

        private readonly CompositeDisposable _disposable = new();

        // このフレームに発生したダメージ（使い回す）
        private readonly List<VirusDamage> _damages = new();

        [Inject]
        public VirusUseCase(
            IVirusDataStore virusDataStore,
            IEnemyDataStore enemyDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IOverclockDataStore overclockDataStore,
            IGameStateDataStore gameStateDataStore
        )
        {
            _virusDataStore = virusDataStore;
            _enemyDataStore = enemyDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _overclockDataStore = overclockDataStore;
            _gameStateDataStore = gameStateDataStore;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyRemoved
                .Subscribe(_virusDataStore.Remove)
                .AddTo(_disposable);
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中（敵が無敵）・オーバークロック中（敵が停止）・ゲームオーバー中（死亡後に撃破させない）は時間を進めない
            if (_waveManagerDataStore.IsWavePause.Value
                || _overclockDataStore.IsActive.CurrentValue
                || _gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            _virusDataStore.Advance(Time.deltaTime, _damages);

            foreach (var virusDamage in _damages)
            {
                // 消えた通知を取りこぼした敵のウイルスが残り続けないよう、ここでも取り除く
                if (!_enemyDataStore.TryGetEnemyData(virusDamage.EnemyId, out _))
                {
                    _virusDataStore.Remove(virusDamage.EnemyId);
                    continue;
                }

                _enemyDataStore.Damage(new HitData(virusDamage.EnemyId, virusDamage.Damage, HitDirectionType.None,
                    isFixedDamage: true));
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
