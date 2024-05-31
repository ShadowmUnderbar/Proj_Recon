using UnityEngine;
using App.Script.Framework.Attributes;

namespace App.Script.Common.Data.MasterData
{
    [CreateAssetMenu(fileName = "EnemyMasterData", menuName = "MasterData/EnemyMasterData")]
    public class EnemyMasterData : ScriptableObject
    {
        [AssetPath(typeof(GameObject))] [SerializeField]
        private string _prefabPath;

        public string PrefabPath => _prefabPath;

        [SerializeField] private string _enemyCode;
        public string EnemyCode => _enemyCode;

        [SerializeField] private float _damage = 1;
        public float Damage => _damage;

        [SerializeField] private float _hp = 10;
        public float Hp => _hp;

        [SerializeField] private float _speed = 1;
        public float Speed => _speed;
    }
}