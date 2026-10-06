using UnityEngine;

namespace App.Common.Views
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

        /// <summary>
        /// 非利き手追従時の向き。指し示す向きへ補正した手のローカル回転で、手に固定する。
        /// 左手向けの値として扱い、非利き手が右のときは鏡写しにする
        /// </summary>
        public readonly Quaternion HandRotation;

        /// <summary>読める面が頭の方を向いているとみなす角度[deg]。視線とメッセージの前方とのなす角がこれ以下なら向いている</summary>
        public readonly float FacingAngle;

        /// <summary>向いている状態から外れるときに <see cref="FacingAngle"/> へ足す余白[deg]。境界での手ぶれによるちらつきを防ぐ</summary>
        public readonly float FacingExitMargin;

        /// <summary>定位置へ追いつく速さ。大きいほど速く、0以下なら補間せず即座に置く</summary>
        public readonly float FollowSpeed;

        public TutorialMessagePlacementSettings(
            float headFollowDuration, Vector3 headOffset, Vector3 handOffset, Quaternion handRotation,
            float facingAngle, float facingExitMargin, float followSpeed)
        {
            HeadFollowDuration = headFollowDuration;
            HeadOffset = headOffset;
            HandOffset = handOffset;
            HandRotation = handRotation;
            FacingAngle = facingAngle;
            FacingExitMargin = facingExitMargin;
            FollowSpeed = followSpeed;
        }
    }
}
