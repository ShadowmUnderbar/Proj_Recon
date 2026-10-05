using App.Common.Data;
using UnityEngine;

namespace App.Common.Interface
{
    /// <summary>
    /// プレイヤーの頭と手の姿勢。チュートリアルメッセージのように、頭や手へ追従させる表示の基準に使う。
    /// バトルとメインメニューでリグが異なるため、シーンごとに実装を差し替える
    /// </summary>
    public interface IPlayerPosePresenter
    {
        /// <summary>手（コントローラ）の姿勢が毎フレーム更新されるか。非VRでは false</summary>
        bool IsHandPoseAvailable { get; }

        /// <summary>頭（VRではHMD、非VRではカメラ）のワールド姿勢を返す。カメラが無ければ false</summary>
        bool TryGetHeadPose(out Pose pose);

        /// <summary>指定した手のコントローラのワールド姿勢（補正前の生の値）</summary>
        Pose GetHandPose(HandType hand);
    }
}
