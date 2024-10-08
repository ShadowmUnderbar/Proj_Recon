using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "BulletDataBase", menuName = "Database/BulletDataBase")]
    public class BulletDataBase : ScriptableObject
    {
        [SerializeField] private BulletData[] _bulletDataBaseList;

        public bool TryGetBulletData(ShotType shotType,bool isFocus, out BulletData bulletData)
        {
            foreach(var bullet in _bulletDataBaseList)
            {
                if(bullet.Type != shotType) 
                { 
                    continue; 
                }

                if(bullet.IsFocus != isFocus)
                {
                    continue;
                }

                bulletData = bullet;
                return true;
            }

            bulletData = null;
            return false;
        }
    }
}