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
        void SetPlayerPose(Pose playerPose);
        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
    }
}