using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        public int Id { get; private set; }

        public ReactiveProperty<Pose> Pose { get; } = new();

        public IHitBoxView[] HitBoxes { get; private set; }
        public EnemyAIBase EnemyAI { get; private set; }

        public void Init(int id, EnemyData enemyData, HitDirectionType resistanceDirectionType)
        {
            Id = id;

            HitBoxes = GetComponentsInChildren<IHitBoxView>();

            foreach (var hitBox in HitBoxes)
            {
                hitBox.Init(Id, HitBoxType.Enemy, resistanceDirectionType);
            }

            EnemyAI = GetComponent<EnemyAIBase>();

            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.Init(enemyData, Id);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            Pose.Value = new Pose(transform.position, transform.rotation);
        }

        public async UniTask Dead()
        {
            EnemyAI.SetState(EnemyAIState.Dead);
        }

        public void SetPlayerTransform(Transform playerTransform)
        {
            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.SetPlayerTransform(playerTransform);
        }

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.SetPlayerAimDirection(aimDir1, aimDir2);
        }
    }
}