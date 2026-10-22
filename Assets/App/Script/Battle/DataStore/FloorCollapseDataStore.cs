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
        public Vector3 Origin { get; private set; }
        public FloorCollapseSettings Settings { get; private set; } = FloorCollapseSettings.Default;

        public void Begin(Vector3 origin, FloorCollapseSettings settings)
        {
            Origin = origin;
            Settings = settings;
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

            // シェーダ（App/FloorCollapse）と同じ式で、波の位置と、そのプレートが動き出す距離を比べる。
            // プレートごとの開始のずれ（乱数）はCPU側では分からないので、平均（ずれ幅の半分）で見る
            var settings = Settings;
            var travel = settings.MaxTravelDistance + settings.Stagger + settings.Jitter;
            var front = _progress.Value / Mathf.Max(settings.BlackoutStart, Mathf.Epsilon) * travel;
            var startOfMotion = settings.Stagger * settings.CrackPortion;
            return front - Distance(worldPosition) - settings.Jitter * 0.5f >= startOfMotion;
        }

        /// <summary>崩壊の波が、その位置に届くまでに進む距離[m]（シェーダの FloorCollapse_Distance と同じ）</summary>
        private float Distance(Vector3 worldPosition)
        {
            var fromOrigin = new Vector2(worldPosition.x - Origin.x, worldPosition.z - Origin.z);
            if (Settings.Mode == FloorCollapseMode.Radial)
            {
                return fromOrigin.magnitude;
            }

            return Vector2.Dot(fromOrigin, Settings.NormalizedDirection) + Settings.MaxTravelDistance * 0.5f;
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
