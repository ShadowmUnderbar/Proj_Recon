using VContainer;
using VContainer.Unity;
using App.Common.Interface;

namespace App.Common.UseCase
{
    public class OVRInputUseCase : IOVRInputUseCase , ITickable
    {
        private readonly IGameInputUsecase _gameInputUsecase;

#if OCULUS
        [Inject]
        public OVRInputUseCase(
            IGameInputUsecase gameInputUsecase
        )
        {
            _gameInputUsecase = gameInputUsecase;
        }
#endif

        public void Tick()
        {
#if OCULUS
            OVRInput.Update();
            OVRInput.FixedUpdate();
            _gameInputUsecase.IsRightTrigger = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            _gameInputUsecase.IsLeftTrigger = OVRInput.Get(OVRInput.RawButton.LIndexTrigger);
            _gameInputUsecase.IsRightGrip = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
            _gameInputUsecase.IsLeftGrip = OVRInput.Get(OVRInput.RawButton.LHandTrigger);

            _gameInputUsecase.IsAButton = OVRInput.Get(OVRInput.RawButton.A);
            _gameInputUsecase.IsBButton = OVRInput.Get(OVRInput.RawButton.B);
            _gameInputUsecase.IsXButton = OVRInput.Get(OVRInput.RawButton.X);
            _gameInputUsecase.IsYButton = OVRInput.Get(OVRInput.RawButton.Y);

            _gameInputUsecase.V2RightAxis = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            _gameInputUsecase.V2LeftAxis = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);

            _gameInputUsecase.IsRightStick = OVRInput.Get(OVRInput.RawButton.RThumbstick);
            _gameInputUsecase.IsLeftStick = OVRInput.Get(OVRInput.RawButton.LThumbstick);
#endif
        }
    }
}