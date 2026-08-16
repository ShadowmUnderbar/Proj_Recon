using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class BattleHitUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBattleHitPresenter _battleHitPresenter;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IHealOnKillDataStore _healOnKillDataStore;
        private readonly IAvalancheDataStore _avalancheDataStore;
        private readonly IMeanMugDataStore _meanMugDataStore;
        private readonly ICriticalHitDataStore _criticalHitDataStore;
        private readonly IElectricShockDataStore _electricShockDataStore;

        private readonly CompositeDisposable _disposable = new();

        // 感電の伝播先。DataStore側のバッファは次の命中で詰め直されるため、適用前にここへ複製する
        private readonly List<int> _electricShockTargets = new();

        [Inject]
        public BattleHitUseCase
        (
            IEnemyDataStore enemyDataStore,
            IBattleHitPresenter battleHitPresenter,
            IEnemyPresenter enemyPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IBuffStateDataStore buffStateDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IHealOnKillDataStore healOnKillDataStore,
            IAvalancheDataStore avalancheDataStore,
            IMeanMugDataStore meanMugDataStore,
            ICriticalHitDataStore criticalHitDataStore,
            IElectricShockDataStore electricShockDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _battleHitPresenter = battleHitPresenter;
            _enemyPresenter = enemyPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _buffStateDataStore = buffStateDataStore;
            _playerStateDataStore = playerStateDataStore;
            _healOnKillDataStore = healOnKillDataStore;
            _avalancheDataStore = avalancheDataStore;
            _meanMugDataStore = meanMugDataStore;
            _criticalHitDataStore = criticalHitDataStore;
            _electricShockDataStore = electricShockDataStore;
        }

        public void Initialize()
        {
            _battleHitPresenter.OnHit
                .Subscribe(OnHit)
                .AddTo(_disposable);

            _enemyDataStore.OnEnemyDead
                .Subscribe(x => OnEnemyDead(x).Forget())
                .AddTo(_disposable);
        }

        private void OnHit(HitData hitData)
        {
            // ウェーブ間ポーズ中は敵を無敵化（ダメージを通さない）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            // 貫通ヒット数に応じたダメージ倍率（PenetrationCount条件バフ）を適用する
            hitData.Damage *= _buffStateDataStore.CalcPenetrationMultiply(hitData.PenetrationIndex);

            // ガン飛ばし: 視界中央に捉えている敵は受ける最終ダメージが増加する
            hitData.Damage *= _meanMugDataStore.GetDamageMultiplier(hitData.DamagedId);

            // クリティカルヒット（ラッキーチャンス・キリングコール・ターンテーブル）を命中ごとに抽選する
            hitData.Damage *= _criticalHitDataStore.GetDamageMultiplier(hitData);

            // マージショットの命中を雪崩へ通知する（次発のクールダウンを短縮する）
            if (hitData.ShotType == ShotType.Merge)
            {
                _avalancheDataStore.NotifyMergeHit();
            }

            // 感電: ワルツ命中時に周囲の敵へダメージを伝播させる。
            // 本命中で対象が撃破される前に伝播先を確定させる（撃破演出中の敵を巻き込まないため）
            var hasChain = _electricShockDataStore.TryGetChain(hitData, out var chain);

            _enemyDataStore.Damage(hitData);

            if (hasChain)
            {
                ApplyElectricShockChain(chain);
            }
        }

        /// <summary>
        /// 感電の伝播ダメージを与える。
        /// ShotTypeは渡さない（射撃そのものではないため、フォーム条件のバフを二重に駆動させない）。
        /// </summary>
        private void ApplyElectricShockChain(ElectricShockChain chain)
        {
            // Damage() は購読者を同期的に走らせるため、その中で伝播先バッファが詰め直されても
            // 取りこぼさないよう、適用前に複製しておく
            _electricShockTargets.Clear();
            _electricShockTargets.AddRange(chain.TargetEnemyIds);

            for (var i = 0; i < _electricShockTargets.Count; i++)
            {
                var enemyId = _electricShockTargets[i];

                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
                {
                    continue;
                }

                var directionType = RelativeYawExtension.GetActorRelative(enemyData.Pose, chain.Center);

                // 伝播の水平方向（命中した敵→伝播先）。傾き演出の向きに使う
                var hitDirection = enemyData.Pose.position - chain.Center;
                hitDirection.y = 0f;
                hitDirection = hitDirection.sqrMagnitude > 0f ? hitDirection.normalized : Vector3.zero;

                _enemyDataStore.Damage(new HitData(enemyId, chain.Damage, directionType, hitDirection));

                // 弾のヒットボックスを経由しないため、被弾の傾き演出は明示的に再生する
                _enemyPresenter.PlayHitFeedback(enemyId, hitDirection);
            }
        }

        private async UniTask OnEnemyDead(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            // 撃破時回復（ジャイアントキリング）: 撃破した敵のランクに応じてプレイヤーを回復する。
            // 死亡演出の await より前に、敵データがまだ存在するうちに処理する。
            if (_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var deadEnemyMasterData))
            {
                var healAmount = _healOnKillDataStore.GetHealAmount(deadEnemyMasterData.EnemyRankType);
                _playerStateDataStore.Heal(healAmount);
            }

            await _enemyPresenter.Dead(enemyId);

            _enemyDataStore.RemoveEnemyData(enemyId);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}