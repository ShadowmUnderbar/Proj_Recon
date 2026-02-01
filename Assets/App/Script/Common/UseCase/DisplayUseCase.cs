using App.Common.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Management;
using VContainer.Unity;

namespace App.Common.UseCase
{
    public class DisplayUseCase : IInitializable
    {
        private void EnterVR()
        {
            EnterVRAsync().Forget();
        }

        private void EnterDesktop()
        {
            EnterDesktopAsync();
        }

        private bool IsVR() => XRGeneralSettings.Instance && XRGeneralSettings.Instance.Manager.activeLoader != null;

        private async UniTaskVoid EnterVRAsync()
        {
            Debug.Log("Initializing XR...");
            await XRGeneralSettings.Instance.Manager.InitializeLoader();
            Debug.Log("Initialized XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        private void EnterDesktopAsync()
        {
            if (!IsVR())
            {
                return;
            }

            Debug.Log("Stopping XR...");

            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }

            Debug.Log("XR stopped completely.");
        }

        public void Initialize()
        {
            if (!DebugConfig.IsVRMode)
            {
                return;
            }

            Application.quitting += EnterDesktop;
            EnterVR();
        }
    }
}