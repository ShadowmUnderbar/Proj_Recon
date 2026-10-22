using App.Battle.Interface;
using App.Battle.Views.Enemy.Bullet;
using System;
using System.Collections.Generic;
using App.Framework.Utilities;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// 場にある弾の置き場。プレハブごとのプール（<see cref="ViewPool{TView}"/>）を持ち、
    /// 貸出（<see cref="IBulletSpawner"/>）と、貸出中の弾への一括操作（停止・オーバークロック・消去・床崩壊）を担う。
    /// 弾は寿命切れ・着弾で自分からプールへ戻る。非表示の弾はこの GameObject の下に置く
    /// </summary>
    public class BulletStoreView : MonoBehaviour, IBulletStoreView, IBulletSpawner
    {
        [SerializeField, Min(0), Tooltip("弾の種類（プレハブ）ごとに最初に作っておく数。足りなければ増える")]
        private int _initialCountPerPrefab = 32;

        private IObjectResolver _resolver;

        // プレハブ → そのプール
        private readonly Dictionary<BaseBulletView, ViewPool<BaseBulletView>> _pools = new();

        // 床崩壊の波に合わせて順に消す弾（BeginSweep の時点で貸出中だったもの）。
        // 返却→再貸出された弾を消さないよう、貸出ごとの通し番号も控える
        private readonly List<(BaseBulletView view, int bulletId)> _sweepTargets = new();

        // 弾の通し番号（貸出ごとに進める。命中の重複判定が弾を見分けるのに使う）
        private int _nextBulletId;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public IBulletView Rent(IBulletView prefab)
        {
            if (prefab is not BaseBulletView bulletPrefab)
            {
                throw new ArgumentException($"弾のプレハブは {nameof(BaseBulletView)} であること: {prefab}", nameof(prefab));
            }

            if (!_pools.TryGetValue(bulletPrefab, out var pool))
            {
                pool = new ViewPool<BaseBulletView>(_resolver, bulletPrefab, transform, _initialCountPerPrefab);
                _pools.Add(bulletPrefab, pool);
            }

            var bullet = pool.Rent();
            bullet.AssignBulletId(++_nextBulletId);
            return bullet;
        }

        public void BeginSweep()
        {
            _sweepTargets.Clear();
            foreach (var pool in _pools.Values)
            {
                foreach (var bullet in pool.Actives)
                {
                    _sweepTargets.Add((bullet, bullet.BulletId));
                }
            }
        }

        public void SweepWhere(Func<Vector3, bool> shouldRemove)
        {
            for (var i = _sweepTargets.Count - 1; i >= 0; i--)
            {
                var (bullet, bulletId) = _sweepTargets[i];

                // 寿命や着弾で先に戻った弾（別の弾として貸し出し直された弾も含む）は外すだけ
                if (!bullet.IsRented || bullet.BulletId != bulletId)
                {
                    _sweepTargets.RemoveAt(i);
                    continue;
                }

                if (!shouldRemove(bullet.transform.position))
                {
                    continue;
                }

                bullet.ReturnToPool();
                _sweepTargets.RemoveAt(i);
            }
        }

        public void SetPause(bool isPause)
        {
            foreach (var pool in _pools.Values)
            {
                foreach (var bullet in pool.Actives)
                {
                    bullet.SetPause(isPause);
                }
            }
        }

        public void SetOverclock(bool isActive)
        {
            // 敵弾の停止・自弾の飛行継続の振り分けは弾側が自分の撃ち手を見て行う
            foreach (var pool in _pools.Values)
            {
                foreach (var bullet in pool.Actives)
                {
                    bullet.SetOverclock(isActive);
                }
            }
        }

        public void AllRemove()
        {
            _sweepTargets.Clear();
            foreach (var pool in _pools.Values)
            {
                pool.ReturnAll();
            }
        }
    }
}
