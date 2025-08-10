using App.Script.Framework.Attributes;
using UnityEngine;

namespace App.Common.Data.MasterData
{
    [CreateAssetMenu(fileName = "EnemyMasterData", menuName = "MasterData/EnemyMasterData")]
    public class EnemyMasterData : ScriptableObject
    {
        [AssetPath(typeof(GameObject))] [SerializeField]
        private string _prefabPath;

        public string PrefabPath => _prefabPath;

        [SerializeField] private string _enemyCode;
        public string EnemyCode => _enemyCode;
        
        [SerializeField] private EnemyRankType _enemyRankType;
        public EnemyRankType EnemyRankType => _enemyRankType;

        [SerializeField] private float _damage = 1;
        public float Damage => _damage;

        [SerializeField] private float _attackInterval = 0.5f;
        public float AttackInterval => _attackInterval;

        [SerializeField] private float _hp = 10;
        public float Hp => _hp;

        [SerializeField] HitDirectionType _resistanceDirectionType;
        public HitDirectionType ResistanceDirectionType => _resistanceDirectionType;

        [SerializeField] private float _resistanceMultiplier = 1f;
        public float ResistanceMultiplier => _resistanceMultiplier;

        [SerializeField] HitDirectionType _weakDirectionType;
        public HitDirectionType WeakDirectionType => _weakDirectionType;

        [SerializeField] private float _weaknessMultiplier = 1f;
        public float WeaknessMultiplier => _weaknessMultiplier;

        [SerializeField] private float _idleSpeed = 1;
        public float IdleSpeed => _idleSpeed;

        [SerializeField] private float _findDistance = 1f;
        public float FindDistance => _findDistance;

        [SerializeField] private float _battleSpeed = 1f;
        public float BattleSpeed => _battleSpeed;

        [SerializeField] private float _bulletSpeed = 17f;
        public float BulletSpeed => _bulletSpeed;

        [SerializeField] private float _attackDistanceRange = 1f;
        public float AttackDistanceRange => _attackDistanceRange;
    }
}