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
        private readonly Subject<Unit> _onCompleted = new();

        public ReadOnlyReactiveProperty<FloorCollapsePhase> Phase => _phase;
        public ReadOnlyReactiveProperty<float> Progress => _progress;
        public Observable<Unit> OnCompleted => _onCompleted;
        public Vector3 Origin { get; private set; }
        public FloorCollapseSettings Settings { get; private set; } = FloorCollapseSettings.Default;

        public void Begin(Vector3 origin, FloorCollapseSettings settings)
        {
            Origin = origin;
            Settings = settings;
            _progress.Value = 0f;
            // 再生中にやり直したときも、購読側が開始をもう一度受け取れるよう一度戻す
            _phase.Value = FloorCollapsePhase.Idle;
            _phase.Value = FloorCollapsePhase.Collapsing;
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
            _onCompleted.Dispose();
        }
    }
}
