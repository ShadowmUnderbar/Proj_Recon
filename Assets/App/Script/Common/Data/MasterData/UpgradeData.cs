using System;
using System.Collections.Generic;
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

        // ショップでの購入に必要なポイント
        [SerializeField, ReadOnlyAttribute] private int _cost;
        public int Cost => _cost;
        
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

        // UpgradeType.GrantBuff のとき付与するバフのID（それ以外は空）
        [Space]
        [SerializeField, ReadOnlyAttribute] private string _buffId;
        public string BuffId => _buffId;

        // 表示用のタグ（UpgradeTagData シートの並び順）。レベル違いのアセットには同じタグが入る
        [Space]
        [SerializeField, ReadOnlyAttribute] private UpgradeTag[] _tags = Array.Empty<UpgradeTag>();
        public IReadOnlyList<UpgradeTag> Tags => _tags ?? Array.Empty<UpgradeTag>();
    }
}