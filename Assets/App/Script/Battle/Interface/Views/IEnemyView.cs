using App.Battle.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyView
    {
        ReactiveProperty<Pose> Pose { get; }
        int Id { get; }
        void Init(int id, EnemyData enemyData);
        UniTask Dead();
    }
}