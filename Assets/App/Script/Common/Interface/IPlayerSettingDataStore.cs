using App.Common.Data;
using R3;

namespace App.Common.Interface
{
    public interface IPlayerSettingDataStore
    {
        ReactiveProperty<HandType> DominantHand { get; }
        HandType NonDominantHand { get; }

        /// <summary>VRでの移動方式</summary>
        ReactiveProperty<LocomotionType> Locomotion { get; }

        /// <summary>スムーズ移動の速度[m/s]</summary>
        ReactiveProperty<float> MoveSpeed { get; }

        /// <summary>スナップターン1回あたりの角度[deg]</summary>
        ReactiveProperty<int> SnapTurnAngle { get; }

        /// <summary>利き手を変更して保存する</summary>
        void SetDominantHand(HandType hand);

        /// <summary>移動方式を変更して保存する</summary>
        void SetLocomotion(LocomotionType locomotion);

        /// <summary>移動速度を変更して保存する。範囲外の値はクランプされる</summary>
        void SetMoveSpeed(float moveSpeed);

        /// <summary>スナップターン角度を変更して保存する。範囲外の値はクランプされる</summary>
        void SetSnapTurnAngle(int angle);
    }
}
