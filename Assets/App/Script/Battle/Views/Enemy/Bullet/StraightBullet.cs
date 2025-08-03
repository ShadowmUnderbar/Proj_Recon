using App.Battle.Interface;
using App.Common.Data;
using App.Framework;
using R3;
using R3.Triggers;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class StraightBullet : MonoBehaviour, IBulletView
    {
        [SerializeField] private Layer _shooterLayer;
        [SerializeField] private Collider _hitCollider;
        private int _attackerId;
        private float _speed;
        private float _damage;

        public void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId)
        {
            _attackerId = attackerId;

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _damage = bulletData.Damage;
            _speed = bulletData.Speed;
            Destroy(gameObject, 5.0f);
        }

        private void Start()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (x.gameObject.layer == _shooterLayer)
                    {
                        return;
                    }

                    if (x.gameObject.CompareTag("Bullet"))
                    {
                        return;
                    }

                    if (!x.TryGetComponent<IHitBoxView>(out var hitBox))
                    {
                        Destroy(gameObject);
                        return;
                    }

                    if (hitBox.Id == _attackerId)
                    {
                        return;
                    }

                    hitBox.OnHit(_damage, _attackerId, transform.position);
                    Destroy(gameObject);
                })
                .AddTo(this);
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);
        }
    }
}