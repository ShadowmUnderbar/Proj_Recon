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

        [SerializeField] private uint _id;
        public uint Id => _id;

        [SerializeField] private float _damage;
        public float Damage => _damage;

        [SerializeField] private float _hp;
        public float Hp => _hp;

        [SerializeField] private float _speed;
        public float Speed => _speed;
    }
}