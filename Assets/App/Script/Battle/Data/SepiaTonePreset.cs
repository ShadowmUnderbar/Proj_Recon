using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// セピア調の演出1つぶんの設定。演出の発生源（オーバークロックなど）ごとに1つ用意する。
    /// 同時に複数かかった場合、グループごとに強いほうが勝つ。
    /// </summary>
    [CreateAssetMenu(fileName = "SepiaTonePreset", menuName = "Config/SepiaTonePreset")]
    public class SepiaTonePreset : ScriptableObject
    {
        [SerializeField, Tooltip("セピア調にするグループ。複数選べる")]
        private SepiaToneGroup _targetGroups = SepiaToneGroup.Background | SepiaToneGroup.Enemy;

        [SerializeField, Range(0f, 1f), Tooltip("セピアの強さ。1で完全なセピア、0で元の色のまま")]
        private float _intensity = 1f;

        [SerializeField, Min(0f), Tooltip("かけ始めてから最大の強さになるまでの秒数。0で即座に切り替える")]
        private float _fadeInSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("解除してから元の色に戻るまでの秒数。0で即座に戻す")]
        private float _fadeOutSeconds = 0.5f;

        public SepiaToneGroup TargetGroups => _targetGroups;
        public float Intensity => _intensity;
        public float FadeInSeconds => _fadeInSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
    }
}
