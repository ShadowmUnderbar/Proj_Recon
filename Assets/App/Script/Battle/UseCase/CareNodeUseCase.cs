using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ケア・ノードの毎秒効果（回復／デメリットダメージ）を駆動する。
    /// 毎フレームではなく1秒ごとに1回だけ適用することで、被弾トリガーのバフが毎フレーム発火するのを防ぐ。
    /// デメリットダメージは <see cref="IPlayerStateDataStore.TakeDamage"/> 経由で与えるため、
    /// 被弾を条件にする他アップグレード（レイジ等）も通常どおり発動する。
    /// </summary>
    public class CareNodeUseCase : ITickable
    {
        // 効果の適用間隔（秒）
        private const float TickInterval = 1f;

        // デメリットダメージで下回らせないHP下限
        private const float MinHealth = 1f;

        private readonly ICareNodeDataStore _careNodeDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private float _elapsed;

        [Inject]
        public CareNodeUseCase(
            ICareNodeDataStore careNodeDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _careNodeDataStore = careNodeDataStore;
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中・ゲームオーバー中は進行させない（被弾処理と同基準）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            while (_elapsed >= TickInterval)
            {
                _elapsed -= TickInterval;
                Apply();
            }
        }

        private void Apply()
        {
            var maxHealth = _playerStateDataStore.MaxHealth.Value;

            if (_careNodeDataStore.TryGetHealPerSecond(maxHealth, out var heal))
            {
                _playerStateDataStore.Heal(heal);
                return;
            }

            if (!_careNodeDataStore.TryGetDamagePerSecond(maxHealth, out var damage))
            {
                return;
            }

            // この効果ではHPを0にしない。残り1以下ならダメージ自体を受けない
            var health = _playerStateDataStore.Health.Value;
            if (health <= MinHealth)
            {
                return;
            }

            _playerStateDataStore.TakeDamage(Mathf.Min(damage, health - MinHealth));
        }
    }
}
