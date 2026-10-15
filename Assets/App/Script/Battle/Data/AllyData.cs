using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 召喚した味方1体の実行時データ
    /// </summary>
    public class AllyData
    {
        public AllyData(int id, AllyType allyType, Vector3 position, float maxHp, float attackInterval,
            float attackRange, bool isTargetable)
        {
            Id = id;
            AllyType = allyType;
            Position = position;
            MaxHp = maxHp;
            Hp = maxHp;
            AttackInterval = attackInterval;
            AttackRange = attackRange;
            IsTargetable = isTargetable;
        }

        /// <summary>味方のId。敵（0以上）・プレイヤー（1）と重ならないよう負の値を使う</summary>
        public int Id { get; }

        public AllyType AllyType { get; }

        /// <summary>出現を要求した位置。実際の位置は View が NavMesh 上へ寄せる</summary>
        public Vector3 Position { get; }

        public float MaxHp { get; }
        public float Hp { get; set; }
        public float AttackInterval { get; }
        public float AttackRange { get; }
        public bool IsTargetable { get; }
    }
}
