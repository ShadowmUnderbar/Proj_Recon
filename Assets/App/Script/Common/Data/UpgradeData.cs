using UnityEngine;

namespace App.Common.Data
{
    [CreateAssetMenu(menuName = "RECON/UpgradeData")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private UpgradeType _upgradeType;
        [SerializeField] private PlayerUnlockType _requiredUnlockType;
        [SerializeField] private float _value;

        public UpgradeType UpgradeType => _upgradeType;
        public PlayerUnlockType RequiredUnlockType => _requiredUnlockType;
        public float Value => _value;
    }
}
