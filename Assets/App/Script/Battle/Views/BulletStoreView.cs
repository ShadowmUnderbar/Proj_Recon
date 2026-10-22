using App.Battle.Interface;
using App.Battle.Views.Enemy.Bullet;
using System;
using System.Collections.Generic;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views
{
    public class BulletStoreView : MonoBehaviour, IBulletStoreView
    {
        // 床崩壊の波に合わせて順に消す弾（BeginSweep の時点で存在したもの）
        private readonly List<GameObject> _sweepTargets = new();

        public void BeginSweep()
        {
            _sweepTargets.Clear();
            _sweepTargets.AddRange(GameObject.FindGameObjectsWithTag(TagConstants.Bullet));
        }

        public void SweepWhere(Func<Vector3, bool> shouldRemove)
        {
            for (var i = _sweepTargets.Count - 1; i >= 0; i--)
            {
                var bullet = _sweepTargets[i];
                // 寿命や着弾で先に消えた弾は外すだけ
                if (bullet == null)
                {
                    _sweepTargets.RemoveAt(i);
                    continue;
                }

                if (!shouldRemove(bullet.transform.position))
                {
                    continue;
                }

                Destroy(bullet);
                _sweepTargets.RemoveAt(i);
            }
        }

        public void SetPause(bool isPause)
        {
            // 弾は動的に生成され続けるため、AllRemoveと同じくTagで集めて一括適用する
            var bullets = GameObject.FindGameObjectsWithTag(TagConstants.Bullet);
            foreach (var bullet in bullets)
            {
                if (!bullet.TryGetComponent<BaseBulletView>(out var bulletView))
                {
                    continue;
                }

                bulletView.SetPause(isPause);
            }
        }

        public void SetOverclock(bool isActive)
        {
            // 敵弾の停止・自弾の飛行継続の振り分けは弾側が自分の撃ち手を見て行う
            var bullets = GameObject.FindGameObjectsWithTag(TagConstants.Bullet);
            foreach (var bullet in bullets)
            {
                if (!bullet.TryGetComponent<BaseBulletView>(out var bulletView))
                {
                    continue;
                }

                bulletView.SetOverclock(isActive);
            }
        }

        public void AllRemove()
        {
            _sweepTargets.Clear();
            // プレイヤー弾・敵弾共に Tag "Bullet" が付いている前提で一括破棄する
            var bullets = GameObject.FindGameObjectsWithTag(TagConstants.Bullet);
            foreach (var bullet in bullets)
            {
                Destroy(bullet);
            }
        }
    }
}
