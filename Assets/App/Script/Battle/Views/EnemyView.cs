using App.Battle.Interface;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        public int Id { get; private set; }

        public ReactiveProperty<Pose> Pose { get; } = new();

        public void Init(int id)
        {
            Id = id;

            var hitboxes = GetComponentsInChildren<HitBoxView>();

            foreach (var hitbox in hitboxes)
            {
                hitbox.Id = Id;
            }
        }

        private void Update()
        {
            Pose.Value = new Pose(transform.position, transform.rotation);
        }


        public async UniTask Dead()
        {
            Destroy(gameObject);
        }
    }
}