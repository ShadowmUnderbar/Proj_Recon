using System;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using UnityEngine.XR.Management;
using VContainer.Unity;

namespace App.Common.UseCase
{
    public class XRInitUseCase : IInitializable, IDisposable
    {
        public void Initialize()
        {
#if UNITY_EDITOR
            if (!DebugConfig.IsVRMode)
            {
                return;
            }
#endif
            InitXR().Forget();
        }

        private static async UniTask InitXR()
        {
            await XRGeneralSettings.Instance.Manager.InitializeLoader();
            while (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                await UniTask.Yield();
            }

            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (!DebugConfig.IsVRMode)
            {
                return;
            }
#endif
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
}