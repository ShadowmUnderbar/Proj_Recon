using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        public int Id { get; private set; }

        public ReactiveProperty<Pose> Pose { get; } = new();

        public IHitBoxView[] HitBoxes { get; private set; }
        public EnemyAIBase EnemyAI { get; private set; }

        public void Init(int id, EnemyData enemyData)
        {
            Id = id;

            HitBoxes = GetComponentsInChildren<IHitBoxView>();

            foreach (var hitBox in HitBoxes)
            {
                hitBox.Id = Id;
            }

            EnemyAI = GetComponent<EnemyAIBase>();

            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.Init(enemyData);
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

        public void SetPlayerPose(Pose playerPose)
        {
            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.SetPlayerPose(playerPose);
        }
    }
}