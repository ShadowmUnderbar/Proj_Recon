using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 配下の<see cref="VrUiRayView"/>を常に有効にする。
    /// バトルではUIの表示中だけレイを出すが（射撃と干渉するため）、
    /// メインメニューのようにUIしかないシーンでは常時出しておきたいので、
    /// XRリグへこれを付けて開始時に有効化する。
    /// 非VRではVrUiRayView側で無効のままになる。
    /// </summary>
    public class VrUiRayAlwaysOnView : MonoBehaviour
    {
        private void Start()
        {
            // VrUiRayViewはAwakeで自身を無効にするため、Startで有効化する
            foreach (var rayView in GetComponentsInChildren<VrUiRayView>(true))
            {
                rayView.SetEnable(true);
            }
        }
    }
}
