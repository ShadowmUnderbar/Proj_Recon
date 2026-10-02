using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

namespace App.Battle.Interface
{
    public interface IEnemyStoreView
    {
        Observable<(int id, Pose pose)> OnEnemyPoseUpdate { get; }

        /// <summary>ボスグループの個体の状態変化（出現時に初期状態も流す）</summary>
        Observable<(int id, BossMemberStatus status)> OnBossMemberStatusChanged { get; }

        /// <summary>ボスグループの個体に行動を命令する（ボスAIでない敵・不在なら何もしない）</summary>
        void CommandBossAction(int enemyId, int actionIndex);

        /// <summary>ボスグループの個体をその場で待機させる／解除する</summary>
        void SetBossHold(int enemyId, bool isHold);
        UniTask Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType);
        void UnSpawn(int enemyId);
        UniTask Dead(int id);
        void AllDeadEnemies();
        /// <summary>
        /// 回避で通過した区間にいる敵のIdを返す。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);

        /// <summary>
        /// 登録中の全敵について、視線（頭の正面へ最大距離までの線分）から敵ごとの判定球までの距離を返す（球が掛かっていれば 0）。
        /// 半径 r の注視判定は「距離 ≤ r」で絞り込む。距離は LateUpdate で数体ずつ順番に更新するため、数フレーム前の値を含む。
        /// まだ判定していない敵は無限大。戻り値は内部リストのため、保持せずその場で使い切る。
        /// </summary>
        IReadOnlyList<(int enemyId, float distanceFromRay)> GetGazeEnemyDistances();

        /// <summary>
        /// 指定半径の球を direction 方向へ distance だけ飛ばし、当たった敵のIdを返す（回避時跳ね返しの直線判定）。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetLineHitEnemies(Vector3 origin, Vector3 direction, float radius, float distance);

        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
        void SetPause(bool isPause);

        /// <summary>敵の移動・行動抽選の速度倍率を設定する（スネークアイズ）</summary>
        void SetSpeedMultiplier(int enemyId, float multiplier);

        /// <summary>敵をスタン状態にする（メデューサ）</summary>
        void SetStun(int enemyId, bool isStun);

        /// <summary>
        /// 敵を指定座標へイージングで移動させる（回避時跳ね返しの吹き飛ばし）。
        /// 経路はNavMesh上に寄せるため、指定座標と完全に一致するとは限らない。
        /// duration が0以下なら瞬間移動する。
        /// </summary>
        void KnockBack(int enemyId, Vector3 destination, float duration);

        /// <summary>被弾の傾き演出を再生する（hitDirection はダメージ源→敵の水平方向）</summary>
        void PlayHitFeedback(int enemyId, Vector3 hitDirection);
    }
}