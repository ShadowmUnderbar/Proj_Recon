using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// オーバークロックの調整パラメータ。
    /// 1回あたりの獲得秒数はアップグレードの Value1 で持ち、ここには発動ルール側の値だけを置く。
    /// </summary>
    [CreateAssetMenu(fileName = "OverclockConfig", menuName = "Config/OverclockConfig")]
    public class OverclockConfig : ScriptableObject
    {
        [SerializeField, Tooltip("ストックがこの秒数を超えた瞬間に発動する（ちょうどでは発動しない）")]
        private float _activationThresholdSeconds = 3f;

        [SerializeField, Tooltip("発動中にかけるセピア調。未指定ならかけない")]
        private SepiaTonePreset _sepiaTonePreset;

        public float ActivationThresholdSeconds => _activationThresholdSeconds;
        public SepiaTonePreset SepiaTonePreset => _sepiaTonePreset;
    }
}
