using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// プレイヤーのバリア耐久値を管理する。
    /// - 被弾時、残量が1以上あれば攻撃を全て吸収し（超過分も破棄）HPを守る
    /// - 最後の被弾から一定時間（RegenDelay）経過後、毎秒 最大値×RegenRatePerSecond ずつ最大値まで回復する
    /// - 最大値は「最大HP × 取得済みバリア倍率(最高レベル)」。取得時は満タンで付与する
    /// </summary>
    public class PlayerBarrierDataStore : IPlayerBarrierDataStore, ITickable
    {
        // 被弾後に回復が始まるまでの待機時間（秒）
        private const float RegenDelay = 7f;

        // 回復開始後、1秒あたりに回復する量（最大値に対する割合）
        private const float RegenRatePerSecond = 0.2f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculator;

        private readonly ReactiveProperty<float> _currentBarrier = new(0f);
        private readonly ReactiveProperty<float> _maxBarrier = new(0f);

        public ReadOnlyReactiveProperty<float> CurrentBarrier => _currentBarrier;
        public ReadOnlyReactiveProperty<float> MaxBarrier => _maxBarrier;

        // 最後に被弾してからの経過時間（秒）
        private float _timeSinceLastDamage;

        [Inject]
        public PlayerBarrierDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculator
        )
        {
            _upgradeEffectSimpleCalculator = upgradeEffectSimpleCalculator;
        }

        public bool TryAbsorb(float damage)
        {
            if (damage <= 0f || _currentBarrier.Value <= 0f)
            {
                return false;
            }

            // バリアが残っていれば攻撃を全て吸収する（超過分もHPには通さない）
            _currentBarrier.Value = Mathf.Max(0f, _currentBarrier.Value - damage);
            return true;
        }

        public void NotifyDamaged()
        {
            _timeSinceLastDamage = 0f;
        }

        public void GrantFull(float maxHealth)
        {
            // 取得済みバリアのうち最高レベルの倍率のみを採用（レベルは累積せず置き換え）
            var ratio = _upgradeEffectSimpleCalculator.CalcMax(UpgradeType.Barrier);
            _maxBarrier.Value = maxHealth * ratio;
            _currentBarrier.Value = _maxBarrier.Value;
            _timeSinceLastDamage = 0f;
        }

        public void Tick()
        {
            // 未取得（最大値0）または既に満タンなら回復不要
            if (_maxBarrier.Value <= 0f || _currentBarrier.Value >= _maxBarrier.Value)
            {
                return;
            }

            _timeSinceLastDamage += Time.deltaTime;
            if (_timeSinceLastDamage < RegenDelay)
            {
                return;
            }

            var regen = _maxBarrier.Value * RegenRatePerSecond * Time.deltaTime;
            _currentBarrier.Value = Mathf.Min(_maxBarrier.Value, _currentBarrier.Value + regen);
        }

        public void Reset()
        {
            _currentBarrier.Value = 0f;
            _maxBarrier.Value = 0f;
            _timeSinceLastDamage = 0f;
        }
    }
}
