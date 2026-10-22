using App.Battle.Interface;
using App.Framework.Utilities;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// プレイヤー・味方の弾の生成窓口。これまでの <see cref="SimpleObjectFactory{TInterface,TView}"/> と同じ
    /// <see cref="ISimpleObjectFactory{TInterface}"/> で、実体は <see cref="IBulletSpawner"/> からの貸出。
    /// 弾は撃ち出した位置に <see cref="IBulletView.Spawn"/> で置くため、親・位置・向きの引数は使わない
    /// </summary>
    public sealed class PooledBulletFactory : ISimpleObjectFactory<IBulletView>
    {
        private readonly IBulletSpawner _bulletSpawner;
        private readonly IBulletView _prefab;

        [Inject]
        public PooledBulletFactory(IBulletSpawner bulletSpawner, PlayerBulletView prefab)
        {
            _bulletSpawner = bulletSpawner;
            _prefab = prefab;
        }

        public IBulletView Instantiate(Transform parent, Vector3 position = default, Quaternion rotation = default)
        {
            return _bulletSpawner.Rent(_prefab);
        }
    }
}
