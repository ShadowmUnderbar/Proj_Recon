using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// 命中（<see cref="IBattleHitPresenter.OnHit"/>）と撃破（<see cref="IEnemyDataStore.OnEnemyDead"/>）の流れ。
    /// アップグレードの効果は登録済みのフック（<see cref="IHitDamageMultiplierHook"/> / <see cref="IHitEffectHook"/> /
    /// <see cref="IEnemyKillHook"/>）を順に呼ぶだけで、個々の効果はここでは知らない。
    /// 命中の順: 倍率フック（<see cref="HitHookOrder"/> の順）→ 副作用フックの前処理 → 感電の伝播先確定
    /// → <see cref="IEnemyDataStore.Damage"/>（弱点・耐性 → 最低保証）→ 副作用フックの後処理 → 感電の伝播。
    /// 感電だけは伝播先に自分でダメージを入れ直す（演出も再生する）ため、フックではなくここで扱う
    /// </summary>
    public class BattleHitUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBattleHitPresenter _battleHitPresenter;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IElectricShockDataStore _electricShockDataStore;

        // 登録順に依存しないよう Order で並べ替えて持つ
        private readonly IHitDamageMultiplierHook[] _damageMultiplierHooks;
        private readonly IHitEffectHook[] _hitEffectHooks;
        private readonly IEnemyKillHook[] _enemyKillHooks;

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
            IPlayerStateDataStore playerStateDataStore,
            IElectricShockDataStore electricShockDataStore,
            IReadOnlyList<IHitDamageMultiplierHook> damageMultiplierHooks,
            IReadOnlyList<IHitEffectHook> hitEffectHooks,
            IReadOnlyList<IEnemyKillHook> enemyKillHooks
        )
        {
            _enemyDataStore = enemyDataStore;
            _battleHitPresenter = battleHitPresenter;
            _enemyPresenter = enemyPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _playerStateDataStore = playerStateDataStore;
            _electricShockDataStore = electricShockDataStore;
            _damageMultiplierHooks = damageMultiplierHooks.OrderBy(hook => hook.Order).ToArray();
            _hitEffectHooks = hitEffectHooks.OrderBy(hook => hook.Order).ToArray();
            _enemyKillHooks = enemyKillHooks.ToArray();
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

            // 倍率は全て乗算（HitHookOrder の順）。弱点・耐性と最低保証は EnemyDataStore.Damage が掛ける
            foreach (var hook in _damageMultiplierHooks)
            {
                hitData.Damage *= hook.GetDamageMultiplier(hitData);
            }

            foreach (var hook in _hitEffectHooks)
            {
                hook.OnBeforeDamage(hitData);
            }

            // 感電: ワルツ命中時に周囲の敵へダメージを伝播させる。
            // 本命中で対象が撃破される前に伝播先を確定させる（撃破演出中の敵を巻き込まないため）
            var hasChain = _electricShockDataStore.TryGetChain(hitData, out var chain);

            // 「実際に与えたダメージ」を使う効果（毒舌）のために、命中前の体力を控えておく
            var healthBefore = _enemyDataStore.TryGetEnemyData(hitData.DamagedId, out var targetBefore) ? targetBefore.Hp : 0f;

            _enemyDataStore.Damage(hitData);

            var isTargetFound = _enemyDataStore.TryGetEnemyData(hitData.DamagedId, out var target);
            var dealtDamage = isTargetFound ? healthBefore - target.Hp : 0f;
            var isTargetAlive = isTargetFound && !target.IsDead;

            foreach (var hook in _hitEffectHooks)
            {
                hook.OnAfterDamage(hitData, dealtDamage, isTargetAlive);
            }

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

            // 撃破時回復（ヴァンパイア・ジャイアントキリング等）は合計してから1回で回復する（体力の書き換えを1回にするため）。
            // 死亡演出の await より前に、敵データがまだ存在するうちに処理する
            var rankType = _enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var deadEnemyMasterData)
                ? deadEnemyMasterData.EnemyRankType
                : EnemyRankType.None;

            var healAmount = 0f;
            foreach (var hook in _enemyKillHooks)
            {
                healAmount += hook.GetHealAmount(rankType);
            }

            _playerStateDataStore.Heal(healAmount);

            // 撃破時の副作用（ラフレシアの伝播等）。
            // 死亡演出がすぐ終わる敵は await の直後に消去され、ウイルスも取り除かれるので、ここで先に呼ぶ
            foreach (var hook in _enemyKillHooks)
            {
                hook.OnEnemyKilled(enemyId);
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
