using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// かかっているセピア調のプリセットと、それぞれのフェードの進み具合を管理する。
    /// グループごとの強さは「そのグループを対象に含むプリセットのうち、いちばん強いもの」にする。
    /// 足し合わせないのは、同じ見た目の演出が重なったときに、効きが倍にならないようにするため。
    /// </summary>
    public class SepiaToneDataStore : ISepiaToneDataStore, IRunResettable, ITickable, IDisposable
    {
        /// <summary>かけている（または外してフェードアウト中の）プリセット1つぶんの状態</summary>
        private sealed class Entry
        {
            public readonly SepiaTonePreset Preset;

            /// <summary>フェードの進み具合。0で効果なし、1で最大</summary>
            public float Progress;

            public bool IsReleased;

            public Entry(SepiaTonePreset preset)
            {
                Preset = preset;
            }
        }

        private readonly List<Entry> _entries = new();

        private readonly ReactiveProperty<Vector4> _weights = new(Vector4.zero);
        public ReadOnlyReactiveProperty<Vector4> Weights => _weights;

        public void Apply(SepiaTonePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            var entry = Find(preset);
            if (entry == null)
            {
                entry = new Entry(preset);
                _entries.Add(entry);
            }

            entry.IsReleased = false;

            // フェード時間0なら次のTickを待たずに反映する
            if (preset.FadeInSeconds <= 0f)
            {
                entry.Progress = 1f;
                UpdateWeights();
            }
        }

        public void Release(SepiaTonePreset preset)
        {
            var entry = Find(preset);
            if (entry == null)
            {
                return;
            }

            entry.IsReleased = true;

            if (preset.FadeOutSeconds <= 0f)
            {
                _entries.Remove(entry);
                UpdateWeights();
            }
        }

        public void Tick()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.Progress = StepProgress(entry, deltaTime);

                if (entry.IsReleased && entry.Progress <= 0f)
                {
                    _entries.RemoveAt(i);
                }
            }

            UpdateWeights();
        }

        public void ResetRun()
        {
            _entries.Clear();
            UpdateWeights();
        }

        public void Dispose()
        {
            _weights.Dispose();
        }

        private Entry Find(SepiaTonePreset preset)
        {
            foreach (var entry in _entries)
            {
                if (entry.Preset == preset)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>フェード時間から、このフレームでの進み具合を求める。フェード時間0は即座に端まで振る</summary>
        private static float StepProgress(Entry entry, float deltaTime)
        {
            var target = entry.IsReleased ? 0f : 1f;
            var seconds = entry.IsReleased ? entry.Preset.FadeOutSeconds : entry.Preset.FadeInSeconds;
            if (seconds <= 0f)
            {
                return target;
            }

            return Mathf.MoveTowards(entry.Progress, target, deltaTime / seconds);
        }

        private void UpdateWeights()
        {
            var weights = Vector4.zero;
            foreach (var entry in _entries)
            {
                var strength = entry.Progress * entry.Preset.Intensity;
                for (var i = 0; i < SepiaToneRenderingLayer.GroupCount; i++)
                {
                    if (((int)entry.Preset.TargetGroups & (1 << i)) != 0)
                    {
                        weights[i] = Mathf.Max(weights[i], strength);
                    }
                }
            }

            // ReactivePropertyは同値なら通知しないので、変化のないフレームではシェーダへの書き込みも起きない
            _weights.Value = weights;
        }
    }
}
