using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 感電で伝播させるダメージの内容。
    /// 命中方向の算出はFrameworkの拡張に依存するため、HitDataの組み立ては呼び出し側（UseCase）で行う。
    /// </summary>
    public readonly struct ElectricShockChain
    {
        public ElectricShockChain(IReadOnlyList<int> targetEnemyIds, float damage, Vector3 center)
        {
            TargetEnemyIds = targetEnemyIds;
            Damage = damage;
            Center = center;
        }

        /// <summary>伝播先の敵Id（命中した敵自身と撃破済みの敵は含まない）</summary>
        public IReadOnlyList<int> TargetEnemyIds { get; }

        /// <summary>伝播先1体あたりに与えるダメージ</summary>
        public float Damage { get; }

        /// <summary>伝播の中心（命中した敵の位置）</summary>
        public Vector3 Center { get; }
    }
}
