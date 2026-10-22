using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 床崩壊の進行度を持つ。秒数から進行度を出し、1に達したら完了を流す。
    /// プレートごとの動きは進行度からシェーダが決めるので、ここでは全体の進行度だけを扱う
    /// </summary>
    public class FloorCollapseDataStore : IFloorCollapseDataStore, IRunResettable, IDisposable
    {
        private readonly ReactiveProperty<FloorCollapsePhase> _phase = new(FloorCollapsePhase.Idle);
        private readonly ReactiveProperty<float> _progress = new(0f);
        private readonly Subject<Unit> _onStarted = new();
        private readonly Subject<Unit> _onCompleted = new();

        public ReadOnlyReactiveProperty<FloorCollapsePhase> Phase => _phase;
        public ReadOnlyReactiveProperty<float> Progress => _progress;
        public Observable<Unit> OnStarted => _onStarted;
        public Observable<Unit> OnCompleted => _onCompleted;
        // 未再生のあいだは既定の設定・原点の波を置いておく（IsCollapsedAt は Phase で守っているので計算には使われない）
        public FloorCollapseWave Wave { get; private set; } = new(Vector3.zero, FloorCollapseSettings.Default);
        public Vector3 Origin => Wave.Origin;
        public FloorCollapseSettings Settings => Wave.Settings;

        public void Begin(Vector3 origin, FloorCollapseSettings settings)
        {
            Wave = new FloorCollapseWave(origin, settings);
            _progress.Value = 0f;
            // 再生中にやり直したときは Phase を Collapsing のまま保つ（Idle を挟むと、ショップの完了待ちなどが打ち切り扱いで消える）。
            // やり直しは OnStarted で知らせる
            _phase.Value = FloorCollapsePhase.Collapsing;
            _onStarted.OnNext(Unit.Default);
        }

        public void Advance(float deltaTime)
        {
            if (_phase.Value != FloorCollapsePhase.Collapsing)
            {
                return;
            }

            SetProgress(_progress.Value + deltaTime / Mathf.Max(Settings.Duration, Mathf.Epsilon));
        }

        public void SetProgress(float progress)
        {
            if (_phase.Value == FloorCollapsePhase.Idle)
            {
                return;
            }

            _progress.Value = Mathf.Clamp01(progress);
            if (_progress.Value < 1f || _phase.Value == FloorCollapsePhase.Completed)
            {
                return;
            }

            _phase.Value = FloorCollapsePhase.Completed;
            _onCompleted.OnNext(Unit.Default);
        }

        public bool IsCollapsedAt(Vector3 worldPosition)
        {
            switch (_phase.Value)
            {
                case FloorCollapsePhase.Idle:
                    return false;
                case FloorCollapsePhase.Completed:
                    return true;
            }

            return Wave.IsCollapsedAt(worldPosition, _progress.Value);
        }

        public void ResetCollapse()
        {
            _progress.Value = 0f;
            _phase.Value = FloorCollapsePhase.Idle;
        }

        public void ResetRun()
        {
            ResetCollapse();
        }

        public void Dispose()
        {
            _phase.Dispose();
            _progress.Dispose();
            _onStarted.Dispose();
            _onCompleted.Dispose();
        }
    }
}
