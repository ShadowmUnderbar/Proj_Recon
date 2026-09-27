using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// チュートリアルメッセージの配置設定。
    /// View の Inspector 値を毎フレーム束ねて渡すことで、Play Mode 中の調整がそのまま効く
    /// </summary>
    public readonly struct TutorialMessagePlacementSettings
    {
        /// <summary>表示開始から視点の正面に追従させる時間[s]</summary>
        public readonly float HeadFollowDuration;

        /// <summary>視点追従時のオフセット[m]。頭のローカル座標（x:右 y:上 z:前）</summary>
        public readonly Vector3 HeadOffset;

        /// <summary>
        /// 非利き手追従時のオフセット[m]。指し示す向きへ補正した手のローカル座標（x:右 y:上 z:前）。
        /// 左手向けの値として扱い、非利き手が右のときは x を反転して左右対称にする
        /// </summary>
        public readonly Vector3 HandOffset;

        /// <summary>定位置へ追いつく速さ。大きいほど速く、0以下なら補間せず即座に置く</summary>
        public readonly float FollowSpeed;

        public TutorialMessagePlacementSettings(
            float headFollowDuration, Vector3 headOffset, Vector3 handOffset, float followSpeed)
        {
            HeadFollowDuration = headFollowDuration;
            HeadOffset = headOffset;
            HandOffset = handOffset;
            FollowSpeed = followSpeed;
        }
    }
}
