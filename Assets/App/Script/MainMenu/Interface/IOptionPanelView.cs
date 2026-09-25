using App.Common.Data;
using R3;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// 部屋に固定設置したオプションパネル。設定値の表示と、操作されたことの通知だけを担う。
    /// 保存はUseCase経由でDataStoreが行う。
    /// </summary>
    public interface IOptionPanelView
    {
        /// <summary>利き手が操作された</summary>
        Observable<HandType> OnDominantHandChanged { get; }

        /// <summary>移動方式が操作された</summary>
        Observable<LocomotionType> OnLocomotionChanged { get; }

        /// <summary>移動速度[m/s]が操作された</summary>
        Observable<float> OnMoveSpeedChanged { get; }

        /// <summary>スナップターン角度[deg]が操作された</summary>
        Observable<int> OnSnapTurnAngleChanged { get; }

        void SetDominantHand(HandType hand);

        void SetLocomotion(LocomotionType locomotion);

        void SetMoveSpeed(float moveSpeed);

        void SetSnapTurnAngle(int angle);
    }
}
