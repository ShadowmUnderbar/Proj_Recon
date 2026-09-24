using System;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 一時停止（フリーズ）の残り時間を管理する。
    /// 秒数は呼び出し側が指定し、0になった時点で自動的に解除する。
    /// </summary>
    public class FreezeDataStore : IFreezeDataStore, ITickable, IDisposable
    {
        private readonly ReactiveProperty<bool> _isFreezing = new(false);
        public ReadOnlyReactiveProperty<bool> IsFreezing => _isFreezing;

        private float _remainingTime;

        public void Freeze(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            // 短い指定で上書きしないよう、残り時間の長い方を採用する
            _remainingTime = Mathf.Max(_remainingTime, duration);
            _isFreezing.Value = true;
        }

        public void Cancel()
        {
            _remainingTime = 0f;
            _isFreezing.Value = false;
        }

        public void Tick()
        {
            if (!_isFreezing.Value)
            {
                return;
            }

            // フリーズ中もこのカウントダウンだけは進める（Time.deltaTimeは止めない方針）
            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0f)
            {
                return;
            }

            Cancel();
        }

        public void Dispose()
        {
            _isFreezing.Dispose();
        }
    }
}
