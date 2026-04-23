using UnityEngine;

namespace App.Common.Data
{
    [CreateAssetMenu(fileName = "RunLevelConfig", menuName = "Config/RunLevelConfig")]
    public class RunLevelConfig : ScriptableObject
    {
        [SerializeField] private float _baseExperience = 50f;
        [SerializeField] private float _experienceExponent = 1.5f;

        [Header("ランク別経験値")]
        [SerializeField] private float _commonExp = 10f;
        [SerializeField] private float _minorExp = 20f;
        [SerializeField] private float _majorExp = 50f;
        [SerializeField] private float _bossExp = 200f;
        [SerializeField] private float _irregularExp = 100f;

        [Header("アップグレード選択")]
        [SerializeField] private int _upgradeChoiceCount = 3;

        public int UpgradeChoiceCount => _upgradeChoiceCount;

        /// <summary>
        /// レベルN→N+1 に必要な経験値
        /// </summary>
        public float GetRequiredExperience(int level) =>
            _baseExperience * Mathf.Pow(level, _experienceExponent);

        public float GetExperienceByRank(EnemyRankType rank) => rank switch
        {
            EnemyRankType.Common => _commonExp,
            EnemyRankType.Minor => _minorExp,
            EnemyRankType.Major => _majorExp,
            EnemyRankType.Boss => _bossExp,
            EnemyRankType.Irregular => _irregularExp,
            _ => 0f
        };
    }
}
