using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// チュートリアルメッセージの追従先となる姿勢のスナップショット。
    /// UseCase が毎フレーム作って View へ渡し、View 側で「視点の正面」→「非利き手の脇」への配置を決める
    /// </summary>
    public readonly struct TutorialMessageAnchor
    {
        /// <summary>頭（VRではHMD、非VRではカメラ）のワールド姿勢</summary>
        public readonly Pose HeadPose;

        /// <summary>追従先の手（非利き手）</summary>
        public readonly HandType Hand;

        /// <summary>追従先の手のコントローラのワールド姿勢（補正前の生の値）</summary>
        public readonly Pose HandPose;

        /// <summary>手の姿勢が使えるか。非VRではコントローラの姿勢が更新されないため false</summary>
        public readonly bool IsHandAvailable;

        public TutorialMessageAnchor(Pose headPose, HandType hand, Pose handPose, bool isHandAvailable)
        {
            HeadPose = headPose;
            Hand = hand;
            HandPose = handPose;
            IsHandAvailable = isHandAvailable;
        }
    }
}
