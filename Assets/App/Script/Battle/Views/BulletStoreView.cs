using App.Battle.Interface;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views
{
    public class BulletStoreView : MonoBehaviour, IBulletStoreView
    {
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
