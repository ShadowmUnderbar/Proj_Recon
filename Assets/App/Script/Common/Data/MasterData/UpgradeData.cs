using UnityEngine;

namespace App.Common.Data.MasterData
{
    [CreateAssetMenu(fileName = "UpgradeMasterData", menuName = "MasterData/UpgradeMasterData")]
    public class UpgradeMasterData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private UpgradeType _upgradeType;
        [SerializeField] private PlayerUnlockType _requiredUnlockType;
        [SerializeField] private int _level;
        [SerializeField] private float _value;

        public string Id => _id;
        public UpgradeType UpgradeType => _upgradeType;
        public PlayerUnlockType RequiredUnlockType => _requiredUnlockType;
        public int Level => _level;
        public float Value => _value;
    }
}
