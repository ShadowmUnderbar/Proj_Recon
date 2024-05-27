using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using UnityEngine.XR.Management;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
        if (!EditorPrefs.GetBool("VRMode", false))
        {
            return;
        }
#endif
        Application.quitting += EnterDesktop;
        EnterVR();
    }
}