using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBattlePlayerView
    {
        Transform PlayerTransform { get; }
        Observable<HitData> OnHit { get; }

        /// <summary>被弾内容を流す（被弾受けコンポーネント由来）</summary>
        Observable<PlayerDamagedData> OnDamaged { get; }

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }
        Observable<Vector3> OnRightAimPosition { get; }
        Observable<Vector3> OnLeftAimPosition { get; }
        ReactiveProperty<Vector3> OnUpdatePosition { get; }
        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }

        /// <summary>
        /// 注視判定の基準になるカメラ（VRではHMD）のPoseを返す。カメラが無ければ false。
        /// </summary>
        bool TryGetGazePose(out Pose pose);

        void IsFocusLeft(bool isFocus);
        void IsFocusRight(bool isFocus);

        void Move(Vector2 inputV2);
        void SetMoveAnimation(Vector2 dir);
        void SetModelFacing(Vector3 dir);
        void SetAimTargets(Vector3 leftTarget, Vector3 rightTarget);
        void Aim();
        void MouseAim(Vector2 mousePos);
        void SetAimRayColor(HandType handType, Color color);
        void SetAimEnableRay(HandType handType, bool enable);
        void SetHandRayColor(HandType handType, Color color);
        void SetHandEnableRay(HandType handType, bool enable);

        /// <summary>VR向けUI操作用のハンドレイ（両手）をまとめて切り替える</summary>
        void SetUiRayEnable(bool enable);

        void Shot(HandType handType, BulletData bulletData, int focusTargetId);

        /// <summary>指定座標へ向けて弾を発射する（パリィのように狙いと無関係な方向へ撃つ用）</summary>
        void ShotToward(HandType handType, BulletData bulletData, int focusTargetId, Vector3 targetPosition);

        void Blitz(Vector3 startPos, Transform playerPos);

        /// <summary>
        /// 即着弾のレイ演出（曳光弾）を再生する。
        /// 射撃を伴わない攻撃（回避時跳ね返し攻撃）から、ノーマル弾と同じ見た目のレイを出すのに使う。
        /// </summary>
        /// <param name="startPos">レイの発射地点</param>
        /// <param name="endPos">レイの着弾地点</param>
        /// <param name="width">レイの太さ（弾の当たり判定サイズと揃える）</param>
        void PlayShotTracer(Vector3 startPos, Vector3 endPos, float width);
    }
}