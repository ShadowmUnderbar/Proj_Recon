using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
using App.Common.Data;
using App.Framework.Utilities.Extensions;
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

        private bool _isPause = false;

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
            EnemyAI.SetPause(_isPause);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!transform.hasChanged)
            {
                return;
            }

            Pose.Value = transform.ToPose();
            transform.hasChanged = false;
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

        public void SetPause(bool isPause)
        {
            _isPause = isPause;

            if (EnemyAI == null)
            {
                return;
            }

            EnemyAI.SetPause(isPause);
        }

        private void OnDestroy()
        {
            Pose.Dispose();
        }
    }
}