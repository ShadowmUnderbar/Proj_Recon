using Unity.Collections;
using UnityEngine;

namespace App.Common.Data.MasterData
{
    [CreateAssetMenu(fileName = "UpgradeMasterData", menuName = "MasterData/UpgradeMasterData")]
    public class UpgradeMasterData : ScriptableObject
    {
        [SerializeField, ReadOnlyAttribute] private string _id;
        public string Id => _id;

        [SerializeField, ReadOnlyAttribute] private string _nameKey;
        public string NameKey => _nameKey;

        [SerializeField, ReadOnlyAttribute] private string _simpleDescriptionKey;
        public string SimpleDescriptionKey => _simpleDescriptionKey;

        [SerializeField, ReadOnlyAttribute] private string _descriptionKey;
        public string DescriptionKey => _descriptionKey;

        [SerializeField, ReadOnlyAttribute] private UpgradeType _upgradeType;
        public UpgradeType UpgradeType => _upgradeType;

        [SerializeField, ReadOnlyAttribute] private PlayerUnlockType _playerUnlockType;
        public PlayerUnlockType PlayerUnlockType => _playerUnlockType;

        [SerializeField, ReadOnlyAttribute] private int _level;
        public int Level => _level;

        [SerializeField, ReadOnlyAttribute] private float _value1;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value1ParameterType;
        public (float value, ParameterType parameterType) Value1 => (_value1, _value1ParameterType);

        [SerializeField, ReadOnlyAttribute] private float _value2;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value2ParameterType;
        public (float value, ParameterType parameterType) Value2 => (_value2, _value2ParameterType);

        [SerializeField, ReadOnlyAttribute] private float _value3;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value3ParameterType;
        public (float value, ParameterType parameterType) Value3 => (_value3, _value3ParameterType);

        [SerializeField, ReadOnlyAttribute] private float _value4;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value4ParameterType;
        public (float value, ParameterType parameterType) Value4 => (_value4, _value4ParameterType);

        [SerializeField, ReadOnlyAttribute] private float _value5;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value5ParameterType;
        public (float value, ParameterType parameterType) Value5 => (_value5, _value5ParameterType);

        public UpgradeMasterData(
            string id,
            string nameKey,
            string simpleDescriptionKey,
            string descriptionKey,
            UpgradeType upgradeType,
            PlayerUnlockType playerUnlockType,
            int level,
            float value1,
            ParameterType value1ParameterType,
            float value2,
            ParameterType value2ParameterType,
            float value3,
            ParameterType value3ParameterType,
            float value4,
            ParameterType value4ParameterType,
            float value5,
            ParameterType value5ParameterType
        )
        {
            _id = id;
            _nameKey = nameKey;
            _simpleDescriptionKey = simpleDescriptionKey;
            _descriptionKey = descriptionKey;
            _upgradeType = upgradeType;
            _playerUnlockType = playerUnlockType;
            _level = level;
            _value1 = value1;
            _value1ParameterType = value1ParameterType;
            _value2 = value2;
            _value2ParameterType = value2ParameterType;
            _value3 = value3;
            _value3ParameterType = value3ParameterType;
            _value4 = value4;
            _value4ParameterType = value4ParameterType;
            _value5 = value5;
            _value5ParameterType = value5ParameterType;
        }
    }
}