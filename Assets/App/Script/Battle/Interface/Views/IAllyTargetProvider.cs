using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>
    /// 敵が狙える味方を探す。敵 AI がプレイヤーと比べて近い方を狙うために使う
    /// </summary>
    public interface IAllyTargetProvider
    {
        /// <summary>
        /// 狙える味方のうち position に最も近いものを返す。いなければ false
        /// </summary>
        bool TryGetNearestTarget(Vector3 position, out Transform target, out float sqrDistance);
    }
}
