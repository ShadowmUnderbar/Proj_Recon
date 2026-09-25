using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Battle.Data
{
    public class EnemyData
    {
        /// <param name="hp">ウェーブ強化を適用済みのHP</param>
        /// <param name="baseDamage">ウェーブ強化を適用済みの攻撃力</param>
        public EnemyData(int id, EnemyMasterData enemyMasterData, Pose pose, float hp, float baseDamage)
        {
            Id = id;
            EnemyMasterDataId = enemyMasterData.EnemyMasterDataId;
            Pose = pose;
            Hp = hp;
            BaseDamage = baseDamage;
            BaseBulletSpeed = enemyMasterData.BulletSpeed;
            IdleSpeed = enemyMasterData.IdleSpeed;
            AttackDistanceRange = enemyMasterData.AttackDistanceRange;
            BattleSpeed = enemyMasterData.BattleSpeed;
            AttackInterval = enemyMasterData.AttackInterval;
            FindDistanceRange = enemyMasterData.FindDistance;
        }

        public int Id { get; set; }
        public string EnemyMasterDataId { get; set; }
        public Pose Pose { get; set; }
        public float Hp { get; set; }

        // 撃破済みフラグ。撃破演出の完了までデータが残るため、
        // その間に届いた追撃（爆風・別の弾）で撃破処理が二重に走るのを防ぐ
        public bool IsDead { get; set; }
        public float BaseDamage { get; set; }
        public float BaseBulletSpeed { get; set; }
        public float BaseBulletSize => 0.4f;
        public float IdleSpeed { get; set; }
        public float AttackDistanceRange { get; set; }
        public float BattleSpeed { get; set; }
        public float AttackInterval { get; set; }
        public float FindDistanceRange { get; set; }
    }
}