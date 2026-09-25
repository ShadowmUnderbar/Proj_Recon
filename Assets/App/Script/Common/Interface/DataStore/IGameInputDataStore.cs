using R3;
using UnityEngine;

namespace App.Common.Interface
{
    public interface IGameInputDataStore
    {
        ReactiveProperty<bool> IsRightTrigger { get; }
        ReactiveProperty<bool> IsLeftTrigger { get; }
        Vector2 V2RightAxis { get; set; }
        Vector2 V2LeftAxis { get; set; }
        ReactiveProperty<bool> IsFocusRight { get; }
        ReactiveProperty<bool> IsFocusLeft { get; }

        /// <summary>グラブ（グリップ）ボタンの押下状態。フォーカス切替とは別に生の入力として参照する</summary>
        ReactiveProperty<bool> IsGrabRight { get; }
        ReactiveProperty<bool> IsGrabLeft { get; }
        ReactiveProperty<bool> IsXButton { get; }
        ReactiveProperty<bool> IsYButton { get; }
        ReactiveProperty<bool> IsRightStick { get; }
        ReactiveProperty<bool> IsLeftStick { get; }
        ReactiveProperty<bool> IsDodge { get; }
        ReactiveProperty<bool> DebugNormal { get; }
        ReactiveProperty<bool> DebugWaltz { get; }
        ReactiveProperty<bool> DebugMerge { get; }
        public Vector2 MouseInputPosition { get; }

        /// <summary>
        /// フォーカス入力の受け付けを切り替える。UI操作でグラブ・トリガーを使う間に
        /// フォーカスが変化しないよう止めるために使う
        /// </summary>
        void SetFocusInputEnable(bool enable);
    }
}