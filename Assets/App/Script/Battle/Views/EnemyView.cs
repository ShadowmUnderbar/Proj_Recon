using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        public int Id { get; private set; }

        public ReactiveProperty<Pose> Pose { get; } = new();

        public void Init(int id, EnemyData enemyData)
        {
            Id = id;

            var hitBoxes = GetComponentsInChildren<HitBoxView>();

            foreach (var hitBox in hitBoxes)
            {
                hitBox.Id = Id;
            }

            var aiBase = GetComponent<EnemyAIBase>();
            aiBase.Init(enemyData);
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