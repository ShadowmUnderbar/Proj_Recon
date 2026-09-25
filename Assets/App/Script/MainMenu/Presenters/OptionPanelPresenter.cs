using App.Common.Data;
using App.MainMenu.Interface;
using R3;
using VContainer;

namespace App.MainMenu.Presenters
{
    public class OptionPanelPresenter : IOptionPanelPresenter
    {
        private readonly IOptionPanelView _optionPanelView;

        public Observable<HandType> OnDominantHandChanged => _optionPanelView.OnDominantHandChanged;
        public Observable<LocomotionType> OnLocomotionChanged => _optionPanelView.OnLocomotionChanged;
        public Observable<float> OnMoveSpeedChanged => _optionPanelView.OnMoveSpeedChanged;
        public Observable<int> OnSnapTurnAngleChanged => _optionPanelView.OnSnapTurnAngleChanged;

        [Inject]
        public OptionPanelPresenter(IOptionPanelView optionPanelView)
        {
            _optionPanelView = optionPanelView;
        }

        public void SetDominantHand(HandType hand) => _optionPanelView.SetDominantHand(hand);

        public void SetLocomotion(LocomotionType locomotion) => _optionPanelView.SetLocomotion(locomotion);

        public void SetMoveSpeed(float moveSpeed) => _optionPanelView.SetMoveSpeed(moveSpeed);

        public void SetSnapTurnAngle(int angle) => _optionPanelView.SetSnapTurnAngle(angle);
    }
}
