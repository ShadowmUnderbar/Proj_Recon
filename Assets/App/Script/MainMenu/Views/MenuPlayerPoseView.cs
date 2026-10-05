using App.Common.Data;
using App.MainMenu.Interface;
using UnityEngine;

namespace App.MainMenu.Views
{
    /// <summary>
    /// メインメニューのリグの頭と両手の姿勢を返す。チュートリアルメッセージの追従先に使う
    /// </summary>
    public class MenuPlayerPoseView : MonoBehaviour, IMenuPlayerPoseView
    {
        [SerializeField, Tooltip("HMD（非VRではメインカメラ）")]
        private Transform _head;

        [SerializeField, Tooltip("左手のコントローラ")]
        private Transform _leftHand;

        [SerializeField, Tooltip("右手のコントローラ")]
        private Transform _rightHand;

        // 非VRではコントローラが動かないため、手への追従は使わない
        public bool IsHandPoseAvailable => DebugConfig.IsVRMode;

        public bool TryGetHeadPose(out Pose pose)
        {
            if (_head == null)
            {
                pose = default;
                return false;
            }

            pose = new Pose(_head.position, _head.rotation);
            return true;
        }

        public Pose GetHandPose(HandType hand)
        {
            var target = hand == HandType.Left ? _leftHand : _rightHand;
            return target == null ? default : new Pose(target.position, target.rotation);
        }
    }
}
