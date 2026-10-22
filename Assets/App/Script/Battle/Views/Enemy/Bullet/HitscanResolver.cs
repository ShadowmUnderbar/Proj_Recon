using System.Collections.Generic;
using App.Battle.Interface;
using App.Common.Data;
using App.Framework;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    /// <summary>
    /// 即着弾（弾速 0）の判定。発射地点から前方へ SphereCast し、
    /// 当たったコライダーを手前から順に並べ、曳光弾の終点と弾道上のポイント粒子も求める。
    /// MonoBehaviour に依存しない plain C# で、判定用のバッファを使い回す（1発ごとに確保しない）。
    /// 弾の GameObject を持たずに「判定して曳光弾だけ出す」撃ち方にもそのまま使える
    /// </summary>
    public sealed class HitscanResolver
    {
        // 1発の判定で拾うコライダーの上限（貫通する敵の数より十分大きい）
        private const int HitBufferSize = 10;

        // 弾道上のポイント粒子の上限
        private const int PointParticleBufferSize = 16;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];
        private readonly RaycastHit[] _pointParticleBuffer = new RaycastHit[PointParticleBufferSize];

        // ヒット結果を距離昇順に並べるための比較子（毎ショットのアロケーション回避のため共有）
        private static readonly IComparer<RaycastHit> HitDistanceComparer =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private int _hitCount;

        /// <summary>直近の <see cref="Cast"/> で拾ったコライダーの数（手前から順）</summary>
        public int HitCount => _hitCount;

        /// <summary>直近の <see cref="Cast"/> のヒット（手前から順。<paramref name="index"/> は 0 始まり）</summary>
        public Collider GetHitCollider(int index) => _hitBuffer[index].collider;

        /// <summary>
        /// 発射地点から前方へ弾の太さで SphereCast し、ヒットを手前から順に並べる。
        /// ポイント粒子は着弾判定に混ぜない（バッファを埋めて本来の対象を押し出すため）。
        /// </summary>
        /// <param name="origin">発射地点</param>
        /// <param name="forward">弾の向き（正規化済み）</param>
        /// <param name="bulletSize">弾の当たり判定の直径</param>
        /// <param name="shooterLayer">撃ち手のレイヤー（曳光弾の終点から除く）</param>
        /// <param name="maxTracerDistance">何にも当たらなかったときに曳光弾を伸ばす距離</param>
        /// <param name="tracerEnd">曳光弾の終点（撃ち手・弾以外で最遠のヒット、無ければ最大距離）</param>
        public void Cast(Vector3 origin, Vector3 forward, float bulletSize, int shooterLayer, float maxTracerDistance,
            out Vector3 tracerEnd)
        {
            _hitCount = Physics.SphereCastNonAlloc(
                origin,
                bulletSize * 0.5f,
                forward,
                _hitBuffer,
                Mathf.Infinity,
                Physics.DefaultRaycastLayers & ~LayerConstants.PointParticle);

            // 曳光弾の着弾地点（撃ち手のレイヤー・弾タグを除いた最遠の有効ヒット）
            var maxHitDistance = -1f;
            for (var i = 0; i < _hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.collider.gameObject.layer == shooterLayer)
                {
                    continue;
                }

                if (hit.collider.gameObject.CompareTag(TagConstants.Bullet))
                {
                    continue;
                }

                if (hit.distance > maxHitDistance)
                {
                    maxHitDistance = hit.distance;
                }
            }

            tracerEnd = maxHitDistance >= 0f
                ? origin + forward * maxHitDistance
                : origin + forward * maxTracerDistance;

            // SphereCastNonAlloc の結果は距離順が保証されないため、手前から順に並べる（貫通順序に依存する効果のため）
            System.Array.Sort(_hitBuffer, 0, _hitCount, HitDistanceComparer);
        }

        /// <summary>
        /// 弾道上にあるポイント粒子を吸い寄せる。
        /// 粒子は Ignore Raycast レイヤーにあり通常のレイキャストには掛からないため、ここで明示的に拾う
        /// </summary>
        /// <returns>吸い寄せを始めた粒子の数</returns>
        public int PullPointParticles(Vector3 startPos, Vector3 endPos, float bulletSize)
        {
            var direction = endPos - startPos;
            var distance = direction.magnitude;
            if (distance <= 0f)
            {
                return 0;
            }

            var hitCount = Physics.SphereCastNonAlloc(
                startPos,
                bulletSize * 0.5f,
                direction / distance,
                _pointParticleBuffer,
                distance,
                LayerConstants.PointParticle,
                QueryTriggerInteraction.Collide);

            var pulled = 0;
            for (var i = 0; i < hitCount; i++)
            {
                if (_pointParticleBuffer[i].collider.TryGetComponent<IPointParticleView>(out var pointParticle))
                {
                    pointParticle.StartPull();
                    pulled++;
                }
            }

            return pulled;
        }
    }
}
