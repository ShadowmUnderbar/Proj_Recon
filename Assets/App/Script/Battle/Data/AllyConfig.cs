using System;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 召喚する味方の種類ごとの調整パラメータ。
    /// 攻撃のダメージはプレイヤーのノーマルショットと同じ計算で決めるため、ここには持たない
    /// </summary>
    [CreateAssetMenu(fileName = "AllyConfig", menuName = "Config/AllyConfig")]
    public class AllyConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private AllyType _allyType;

            [SerializeField, Min(1f), Tooltip("基礎の最大体力。プレイヤーの体力アップと同じ倍率が掛かる。0になると撃破されて消える")]
            private float _maxHp = 100f;

            [SerializeField, Min(0.05f), Tooltip("攻撃の間隔[秒]")]
            private float _attackInterval = 0.5f;

            [SerializeField, Min(0f), Tooltip("この距離[m]以内の敵を狙う")]
            private float _attackRange = 15f;

            [SerializeField, Min(0f), Tooltip("狙う敵へ向き直る速さ[度/秒]。0なら即座に向く")]
            private float _turnSpeed = 15f;

            [SerializeField, Min(0f), Tooltip("移動の最高速度[m/秒]。動かない味方（タレット）は使わない")]
            private float _moveSpeed;

            [SerializeField, Min(0), Tooltip("同時に出せる最大数。0なら上限なし。上限に達している間は召喚しない")]
            private int _maxCount;

            [SerializeField, Tooltip("敵に狙われるか。false なら敵は狙わず、敵の攻撃も当たらない")]
            private bool _isTargetable = true;

            public AllyType AllyType => _allyType;
            public float MaxHp => _maxHp;
            public float AttackInterval => _attackInterval;
            public float AttackRange => _attackRange;
            public float TurnSpeed => _turnSpeed;
            public float MoveSpeed => _moveSpeed;
            public int MaxCount => _maxCount;
            public bool IsTargetable => _isTargetable;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public bool TryGetEntry(AllyType allyType, out Entry entry)
        {
            foreach (var e in _entries)
            {
                if (e != null && e.AllyType == allyType)
                {
                    entry = e;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
