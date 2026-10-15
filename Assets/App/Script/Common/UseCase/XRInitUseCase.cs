using System;
using App.Common.Data;
using App.Common.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Management;
using VContainer.Unity;

namespace App.Common.UseCase
{
    public class XRInitUseCase : IXRInitUseCase, IInitializable, IDisposable
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
#if UNITY_EDITOR
            if (!DebugConfig.IsVRMode)
            {
                IsInitialized = true;
                return;
            }
#endif
            InitXR().Forget();
        }

        private async UniTask InitXR()
        {
            try
            {
                await XRGeneralSettings.Instance.Manager.InitializeLoader();
                while (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    await UniTask.Yield();
                }

                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
            catch (Exception e)
            {
                Debug.LogError($"[XRInitUseCase] XR の起動に失敗しました\n{e}");
            }
            finally
            {
                // 失敗しても起動シーンが先へ進めるように、終わったことだけは知らせる
                IsInitialized = true;
            }
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
