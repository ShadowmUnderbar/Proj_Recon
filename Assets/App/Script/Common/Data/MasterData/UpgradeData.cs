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

        [SerializeField, ReadOnlyAttribute] private UpgradeType _upgradeType;
        public UpgradeType UpgradeType => _upgradeType;

        [SerializeField, ReadOnlyAttribute] private PlayerUnlockType _playerUnlockType;
        public PlayerUnlockType PlayerUnlockType => _playerUnlockType;

        [SerializeField, ReadOnlyAttribute] private int _level;
        public int Level => _level;
        
        [Space]
        [SerializeField, ReadOnlyAttribute] private float _value1;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value1ParameterType;
        public (float value, ParameterType parameterType) Value1 => (_value1, _value1ParameterType);

        [Space]
        [SerializeField, ReadOnlyAttribute] private float _value2;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value2ParameterType;
        public (float value, ParameterType parameterType) Value2 => (_value2, _value2ParameterType);

        [Space]
        [SerializeField, ReadOnlyAttribute] private float _value3;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value3ParameterType;
        public (float value, ParameterType parameterType) Value3 => (_value3, _value3ParameterType);

        [Space]
        [SerializeField, ReadOnlyAttribute] private float _value4;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value4ParameterType;
        public (float value, ParameterType parameterType) Value4 => (_value4, _value4ParameterType);

        [Space]
        [SerializeField, ReadOnlyAttribute] private float _value5;
        [SerializeField, ReadOnlyAttribute] private ParameterType _value5ParameterType;
        public (float value, ParameterType parameterType) Value5 => (_value5, _value5ParameterType);

        [Space]
        // パッシブ発動条件。None なら従来の恒久アップグレードとして常に適用される。
        [SerializeField, ReadOnlyAttribute] private ConditionType _conditionType;
        public ConditionType ConditionType => _conditionType;

        // 条件のしきい値／継続秒数。意味は ConditionType ごとに異なる（ConditionType の定義を参照）。
        [SerializeField, ReadOnlyAttribute] private float _conditionValue;
        public float ConditionValue => _conditionValue;

        public UpgradeMasterData(
            string id,
            string nameKey,
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
            ParameterType value5ParameterType,
            ConditionType conditionType,
            float conditionValue
        )
        {
            _id = id;
            _nameKey = nameKey;
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
            _conditionType = conditionType;
            _conditionValue = conditionValue;
        }
    }
}