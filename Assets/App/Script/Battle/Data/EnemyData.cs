using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Battle.Data
{
    public class EnemyData
    {
        public int Id { get; set; }
        public EnemyMasterData MasterData { get; set; }
        public float Hp { get; set; }
        public float Damage { get; set; }
        public float IdleSpeed { get; set; }
        public float BattleSpeed { get; set; }
        public Pose Pose { get; set; }
        public float AttackDistanceRange { get; set; }
        public float FindDistanceRange { get; set; }
        public float AttackInterval { get; set; }
    }
}