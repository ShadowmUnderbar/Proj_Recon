using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// アップグレードカードの掴み操作に使う、片手ぶんの入力スナップショット。
    /// UseCase が毎フレーム作って View へ渡し、View 側で掴み・追従・確定の判定に使う
    /// </summary>
    public readonly struct ShopHandInput
    {
        /// <summary>どちらの手か</summary>
        public readonly HandType HandType;

        /// <summary>コントローラのワールド姿勢</summary>
        public readonly Pose Pose;

        /// <summary>グラブ（グリップ）ボタンを握っているか</summary>
        public readonly bool IsGrabbing;

        /// <summary>トリガーボタンを引いているか（掴んでいるカードの確定に使う）</summary>
        public readonly bool IsConfirming;

        public ShopHandInput(HandType handType, Pose pose, bool isGrabbing, bool isConfirming)
        {
            HandType = handType;
            Pose = pose;
            IsGrabbing = isGrabbing;
            IsConfirming = isConfirming;
        }
    }
}
