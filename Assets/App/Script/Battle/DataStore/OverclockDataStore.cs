using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// オーバークロックのストック秒数・発動状態・溜めたダメージを管理する。
    /// 発動中の残り時間は自身の Tick で減らし、0になった時点で自動的に終了する。
    /// </summary>
    public class OverclockDataStore : IOverclockDataStore, IRunResettable, ITickable, IDisposable
    {
        private readonly OverclockConfig _config;
        private readonly IFreezeDataStore _freezeDataStore;

        private readonly ReactiveProperty<float> _stockSeconds = new(0f);
        public ReadOnlyReactiveProperty<float> StockSeconds => _stockSeconds;

        private readonly ReactiveProperty<bool> _isActive = new(false);
        public ReadOnlyReactiveProperty<bool> IsActive => _isActive;

        public float RemainingTime { get; private set; }

        private float _stockedDamage;

        [Inject]
        public OverclockDataStore(OverclockConfig config, IFreezeDataStore freezeDataStore)
        {
            _config = config;
            _freezeDataStore = freezeDataStore;
        }

        public void AddStock(float seconds)
        {
            if (seconds <= 0f || _isActive.Value)
            {
                return;
            }

            _stockSeconds.Value += seconds;

            if (_stockSeconds.Value <= _config.ActivationThresholdSeconds)
            {
                return;
            }

            // ストックをすべて効果時間に変換して発動する
            RemainingTime = _stockSeconds.Value;
            _stockSeconds.Value = 0f;
            _isActive.Value = true;
        }

        public void AddStockedDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            _stockedDamage += damage;
        }

        public float ConsumeStockedDamage()
        {
            var damage = _stockedDamage;
            _stockedDamage = 0f;
            return damage;
        }

        public void ForceEnd()
        {
            RemainingTime = 0f;
            _isActive.Value = false;
        }

        public void ResetRun()
        {
            // 溜めたダメージは終了通知より先に捨てる（リセットで持ち越しダメージを受けないように）
            _stockedDamage = 0f;
            _stockSeconds.Value = 0f;
            ForceEnd();
        }

        public void Tick()
        {
            if (!_isActive.Value)
            {
                return;
            }

            // フリーズ（ヒットストップ）中はプレイヤーも動けないため、発動時間を消費させない
            if (_freezeDataStore.IsFreezing.CurrentValue)
            {
                return;
            }

            RemainingTime -= Time.deltaTime;
            if (RemainingTime > 0f)
            {
                return;
            }

            ForceEnd();
        }

        public void Dispose()
        {
            _stockSeconds.Dispose();
            _isActive.Dispose();
        }
    }
}
