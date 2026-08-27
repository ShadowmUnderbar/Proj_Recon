using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyPresenter
    {
        Observable<(int id, Pose pose)> OnEnemyPoseUpdate { get; }
        void Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType);
        void UnSpawn(int enemyId);
        void RemoveAllEnemies();

        /// <summary>
        /// 回避で通過した区間にいる敵のIdを返す。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);

        /// <summary>
        /// 視界中央から半径 radius のレイを飛ばし、捉えた敵のIDを返す。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetGazeEnemies(Vector3 origin, Vector3 direction, float radius, float distance);

        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
        UniTask Dead(int id);
        void SetPause(bool isPause);

        /// <summary>敵の移動・行動抽選の速度倍率を設定する（スネークアイズ）</summary>
        void SetSpeedMultiplier(int enemyId, float multiplier);

        /// <summary>敵をスタン状態にする（メデューサ）</summary>
        void SetStun(int enemyId, bool isStun);

        /// <summary>
        /// 敵を指定座標へ押し出す（回避時跳ね返し攻撃で巻き込んだ敵を回避方向へ移動させる）。
        /// NavMesh上の最も近い地点へ移動させるため、指定座標と完全に一致するとは限らない。
        /// </summary>
        void Push(int enemyId, Vector3 position);

        /// <summary>
        /// 被弾の傾き演出を再生する（感電のように弾のヒットボックスを経由しないダメージ用）。
        /// hitDirection は「ダメージ源→敵」の水平方向。
        /// </summary>
        void PlayHitFeedback(int enemyId, Vector3 hitDirection);
    }
}