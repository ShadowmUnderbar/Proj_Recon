using System;
using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ボスによる時止めの状態を持つ。始める・解くはボスの台本（<c>BossGroupUseCase</c> 経由）が行う。
    /// 秒数では解かない（台本が予兆を見せ終えたときに解く）。
    /// </summary>
    public class TimeStopDataStore : ITimeStopDataStore, IRunResettable, IDisposable
    {
        private readonly ReactiveProperty<bool> _isTimeStopped = new(false);
        public ReadOnlyReactiveProperty<bool> IsTimeStopped => _isTimeStopped;

        public void SetTimeStop(bool isTimeStopped)
        {
            _isTimeStopped.Value = isTimeStopped;
        }

        public void ResetRun()
        {
            _isTimeStopped.Value = false;
        }

        public void Dispose()
        {
            _isTimeStopped.Dispose();
        }
    }
}
