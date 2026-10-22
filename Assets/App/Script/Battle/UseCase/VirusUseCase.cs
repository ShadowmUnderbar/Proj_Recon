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
    /// 敵が消えたらウイルスも取り除く（撃破の通知の時点では残っている。ただし死亡演出がすぐ終わる敵は同じ通知の中で消えるので、
    /// 撃破時に読む処理は BattleHitUseCase.OnEnemyDead の死亡演出の await より前に置く）。
    /// マリスもここで駆動する（範囲内の敵の残り効果時間を止める指定は、直後の Advance にだけ効くため）。
    /// デバッグ設定でログを有効にすると、発生・重複の状態と1回ごとのダメージ量をログに出す
    /// </summary>
    public class VirusUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IVirusDataStore _virusDataStore;
        private readonly IMaliceDataStore _maliceDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IOverclockDataStore _overclockDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly VirusDebugSettings _debugSettings;

        private readonly CompositeDisposable _disposable = new();

        // このフレームに発生したダメージ（使い回す）
        private readonly List<VirusDamage> _damages = new();

        [Inject]
        public VirusUseCase(
            IVirusDataStore virusDataStore,
            IMaliceDataStore maliceDataStore,
            IEnemyDataStore enemyDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IOverclockDataStore overclockDataStore,
            IGameStateDataStore gameStateDataStore,
            VirusDebugSettings debugSettings
        )
        {
            _virusDataStore = virusDataStore;
            _maliceDataStore = maliceDataStore;
            _enemyDataStore = enemyDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _overclockDataStore = overclockDataStore;
            _gameStateDataStore = gameStateDataStore;
            _debugSettings = debugSettings;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyRemoved
                .Subscribe(_virusDataStore.Remove)
                .AddTo(_disposable);

            if (_debugSettings.LogEnabled)
            {
                _virusDataStore.OnApplied
                    .Subscribe(LogApplied)
                    .AddTo(_disposable);
            }
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

            // マリス: 範囲内の敵の残り効果時間を止め、1秒ごとに威力を足す。止める指定は直後の Advance にだけ効く
            _maliceDataStore.Tick(Time.deltaTime);

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

                if (_debugSettings.LogEnabled)
                {
                    LogDamage(virusDamage);
                }
            }
        }

        private void LogApplied(VirusApplyResult result)
        {
            var after = result.After;
            var damagePerSecond = result.DamagePerSecond;

            if (result.IsNew)
            {
                Debug.Log($"[Virus] 発生 敵={result.EnemyId} 付与(威力={result.AddedPower}, 効果時間={result.AddedDuration}秒) "
                          + $"→ 威力={after.Power} 最長効果時間={after.MaxDuration}秒 残り効果時間={after.RemainingTime:F2}秒 "
                          + $"毎秒ダメージ={damagePerSecond}");
                return;
            }

            var before = result.Before;
            // 威力だけ足す付与（AddPower）は効果時間を渡さないので、0秒と出さずにそう書く
            var added = result.AddedDuration > 0f
                ? $"威力={result.AddedPower}, 効果時間={result.AddedDuration}秒"
                : $"威力={result.AddedPower}, 威力だけ加算";
            Debug.Log($"[Virus] 重複 敵={result.EnemyId} 付与({added}) "
                      + $"威力 {before.Power}→{after.Power} / 最長効果時間 {before.MaxDuration}→{after.MaxDuration}秒 / "
                      + $"残り効果時間 {before.RemainingTime:F2}→{after.RemainingTime:F2}秒"
                      + $"（時間は{(result.IsDurationReplaced ? "置き換え" : "そのまま")}） 毎秒ダメージ={damagePerSecond}");
        }

        private void LogDamage(VirusDamage virusDamage)
        {
            var hasEnemy = _enemyDataStore.TryGetEnemyData(virusDamage.EnemyId, out var enemy);
            var hp = hasEnemy ? enemy.Hp : 0f;

            // 最後の1回はダメージと同じフレームで効果が切れて取り除かれている。
            // 体力共有のボスでは、このダメージで仲間ごと消えて取り除かれることもある
            var state = _virusDataStore.TryGetVirus(virusDamage.EnemyId, out var status)
                ? $"威力={status.Power} 最長効果時間={status.MaxDuration}秒 残り効果時間={status.RemainingTime:F2}秒"
                : hasEnemy ? "効果終了" : "敵が消えた";

            Debug.Log($"[Virus] ダメージ 敵={virusDamage.EnemyId} ダメージ={virusDamage.Damage} 残りHP={hp}（{state}）");
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
