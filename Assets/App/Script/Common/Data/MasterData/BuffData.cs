using Unity.Collections;
using UnityEngine;

namespace App.Common.Data.MasterData
{
    [CreateAssetMenu(fileName = "BuffMasterData", menuName = "MasterData/BuffMasterData")]
    public class BuffMasterData : ScriptableObject
    {
        [SerializeField, ReadOnlyAttribute] private string _id;
        public string Id => _id;

        [SerializeField, ReadOnlyAttribute] private string _nameKey;
        public string NameKey => _nameKey;

        [SerializeField, ReadOnlyAttribute] private BuffConditionType _conditionType;
        public BuffConditionType ConditionType => _conditionType;

        // 条件の閾値（HitCount: 必要ヒット数 / HpBelow: HP割合 0〜1）
        [SerializeField, ReadOnlyAttribute] private float _conditionValue;
        public float ConditionValue => _conditionValue;

        // 効果時間（秒）。0以下なら条件成立中のみ有効
        [SerializeField, ReadOnlyAttribute] private float _duration;
        public float Duration => _duration;

        [SerializeField, ReadOnlyAttribute] private BuffEffectType _effectType;
        public BuffEffectType EffectType => _effectType;

        // 効果値（倍率）
        [SerializeField, ReadOnlyAttribute] private float _effectValue;
        public float EffectValue => _effectValue;
    }
}
