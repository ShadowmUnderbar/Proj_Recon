using App.Battle.Data;
using App.Battle.Interface.EnemyAI;
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
        void Init(int id, EnemyData enemyData);
        void Destroy();
        UniTask Dead();
        void SetPlayerPose(Pose playerPose);
    }
}