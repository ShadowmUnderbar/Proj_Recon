using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// 味方が狙う敵を探す（plain C#）。範囲内の敵のヒットボックスのうち中心が最も近いものを返す。
    /// 判定バッファは使い回すため、1体の味方につき1つ持つ
    /// </summary>
    public class AllyEnemySearcher
    {
        private readonly Collider[] _buffer;

        public AllyEnemySearcher(int bufferSize = 32)
        {
            _buffer = new Collider[bufferSize];
        }

        public bool TryFindNearest(Vector3 origin, float range, out Collider target)
        {
            target = null;

            var count = Physics.OverlapSphereNonAlloc(origin, range, _buffer, LayerConstants.Enemy,
                QueryTriggerInteraction.Collide);

            var nearestSqrDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var col = _buffer[i];
                if (!col.TryGetComponent<IHitBoxView>(out var hitBox) || hitBox.HitBoxType != HitBoxType.Enemy)
                {
                    continue;
                }

                var center = col.bounds.center;
                var sqrDistance = (center - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                target = col;
            }

            return target != null;
        }
    }
}
