using App.Battle.Data;
using App.Battle.Interface.EnemyAI;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyView
    {
        IHitBoxView[] HitBoxes { get; }
        ReactiveProperty<Pose> Pose { get; }
        int Id { get; }
        void Init(int id, EnemyData enemyData, HitDirectionType resistanceDirectionType);
        void Destroy();
        UniTask Dead();
        void SetPlayerTransform(Transform playerTransform);
        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
        void SetPause(bool isPause);

        /// <summary>移動・行動抽選の速度倍率を設定する（スネークアイズ）</summary>
        void SetSpeedMultiplier(float multiplier);

        /// <summary>スタン状態を設定する（メデューサ）</summary>
        void SetStun(bool isStun);

        /// <summary>被弾の傾き演出を再生する（hitDirection はダメージ源→敵の水平方向）</summary>
        void PlayHitFeedback(Vector3 hitDirection);
    }
}