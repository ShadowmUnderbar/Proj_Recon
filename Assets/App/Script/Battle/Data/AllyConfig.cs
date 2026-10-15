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

            [SerializeField, Min(1f), Tooltip("最大体力。0になると撃破されて消える")]
            private float _maxHp = 100f;

            [SerializeField, Min(0.05f), Tooltip("攻撃の間隔[秒]")]
            private float _attackInterval = 0.5f;

            [SerializeField, Min(0f), Tooltip("この距離[m]以内の敵を狙う")]
            private float _attackRange = 15f;

            [SerializeField, Tooltip("敵に狙われるか。false なら敵は狙わず、敵の攻撃も当たらない")]
            private bool _isTargetable = true;

            public AllyType AllyType => _allyType;
            public float MaxHp => _maxHp;
            public float AttackInterval => _attackInterval;
            public float AttackRange => _attackRange;
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
