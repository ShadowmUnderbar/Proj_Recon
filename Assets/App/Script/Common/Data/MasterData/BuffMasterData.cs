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

        [SerializeField, ReadOnlyAttribute] private BuffType _buffType;
        public BuffType BuffType => _buffType;

        // 効果時間（秒）。0以下なら時間制限なし（手動除去のみ）
        [SerializeField, ReadOnlyAttribute] private float _duration;
        public float Duration => _duration;
        public bool HasDuration => _duration > 0f;

        // 正値=バフ(UP)、負値=デバフ(DOWN)。例: 0.2→+20%、-0.15→-15%
        [SerializeField, ReadOnlyAttribute] private float _baseValue;
        public float BaseValue => _baseValue;

        // 最大スタック数
        [SerializeField, ReadOnlyAttribute] private int _maxStack;
        public int MaxStack => _maxStack;
    }
}
