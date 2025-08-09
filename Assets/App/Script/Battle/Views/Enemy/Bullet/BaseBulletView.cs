using App.Battle.Interface;
using App.Common.Data;
using App.Framework;
using R3;
using R3.Triggers;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class BaseBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private Collider _hitCollider;
        [SerializeField] private Layer _shooterLayer;

        protected float Speed;
        protected Transform TargetTransform;

        private int _attackerId;
        private float _damage;

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (x.gameObject.layer == _shooterLayer)
                    {
                        Debug.Log("BaseBulletView:Hit - Shooter Layer");
                        return;
                    }

                    if (x.gameObject.CompareTag("Bullet"))
                    {
                        Debug.Log("BaseBulletView:Hit - Bullet");

                        return;
                    }

                    if (!x.TryGetComponent<IHitBoxView>(out var hitBox))
                    {
                        Debug.Log("BaseBulletView:Hit - No HitBox" + x.gameObject.name);
                        Destroy(gameObject);
                        return;
                    }

                    if (hitBox.Id == _attackerId)
                    {
                        Debug.Log("BaseBulletView:Hit - AttackerId");
                        return;
                    }

                    hitBox.OnHit(_damage, _attackerId, transform.position, out _);
                    Destroy(gameObject);
                })
                .AddTo(this);
        }

        protected virtual void Update()
        {
        }

        public virtual void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId,
            Transform targetTransform = null)
        {
            _attackerId = attackerId;

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _damage = bulletData.Damage;
            Speed = bulletData.Speed;
        }
    }
}