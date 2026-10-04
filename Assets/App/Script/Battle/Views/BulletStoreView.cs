using App.Battle.Interface;
using App.Battle.Views.Enemy.Bullet;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views
{
    public class BulletStoreView : MonoBehaviour, IBulletStoreView
    {
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
            // プレイヤー弾・敵弾共に Tag "Bullet" が付いている前提で一括破棄する
            var bullets = GameObject.FindGameObjectsWithTag(TagConstants.Bullet);
            foreach (var bullet in bullets)
            {
                Destroy(bullet);
            }
        }
    }
}
