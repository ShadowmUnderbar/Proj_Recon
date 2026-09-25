using App.Common.Data;
using R3;

namespace App.MainMenu.Interface
{
    public interface IOptionPanelPresenter
    {
        Observable<HandType> OnDominantHandChanged { get; }

        Observable<LocomotionType> OnLocomotionChanged { get; }

        Observable<float> OnMoveSpeedChanged { get; }

        Observable<int> OnSnapTurnAngleChanged { get; }

        void SetDominantHand(HandType hand);

        void SetLocomotion(LocomotionType locomotion);

        void SetMoveSpeed(float moveSpeed);

        void SetSnapTurnAngle(int angle);
    }
}
