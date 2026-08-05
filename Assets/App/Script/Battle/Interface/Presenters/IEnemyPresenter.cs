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

        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);

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
    }
}