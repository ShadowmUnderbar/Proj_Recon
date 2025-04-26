using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "BulletDataBase", menuName = "Database/BulletDataBase")]
    public class BulletDataBase : ScriptableObject
    {
        [SerializeField] private BulletData[] _bulletDataBaseList;

        public bool TryGetBulletData(ShotType shotType, AimFocusType focusType, out BulletData bulletData)
        {
            foreach (var bullet in _bulletDataBaseList)
            {
                if (bullet.Shotype != shotType)
                {
                    continue;
                }

                if (bullet.FocusType != focusType)
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