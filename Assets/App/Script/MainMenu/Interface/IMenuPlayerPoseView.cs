using App.Common.Data;
using UnityEngine;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// メインメニューのリグ（HMDと両手のコントローラ）の姿勢
    /// </summary>
    public interface IMenuPlayerPoseView
    {
        /// <summary>手（コントローラ）の姿勢が毎フレーム更新されるか。非VRでは false</summary>
        bool IsHandPoseAvailable { get; }

        /// <summary>頭（VRではHMD、非VRではカメラ）のワールド姿勢を返す。未設定なら false</summary>
        bool TryGetHeadPose(out Pose pose);

        /// <summary>指定した手のコントローラのワールド姿勢</summary>
        Pose GetHandPose(HandType hand);
    }
}
