using System;
using System.Collections.Generic;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 複数の個体で構成するボスの定義。出現させる個体と、それらを動かす行動台本を持つ。
    /// 台本は先頭から順に進め、末尾まで進んだら先頭へ戻って繰り返す。
    /// </summary>
    [CreateAssetMenu(fileName = "BossGroupConfig", menuName = "MasterData/BossGroupConfig")]
    public class BossGroupConfig : ScriptableObject
    {
        [Serializable]
        public class Member
        {
            [SerializeField, Tooltip("出現させる敵（AIはプレハブに付けた BossAIBase 派生で決まる）")]
            private EnemyMasterData _enemyMasterData;

            [SerializeField, Tooltip("出現位置の基準点からのずれ（基準の向きに対するローカル座標、m）")]
            private Vector3 _spawnOffset;

            public EnemyMasterData EnemyMasterData => _enemyMasterData;
            public Vector3 SpawnOffset => _spawnOffset;
        }

        [SerializeField] private Member[] _members = Array.Empty<Member>();

        [SerializeField, Tooltip("行動台本。末尾まで進んだら先頭から繰り返す")]
        private BossPatternStep[] _pattern = Array.Empty<BossPatternStep>();

        public IReadOnlyList<Member> Members => _members;
        public IReadOnlyList<BossPatternStep> Pattern => _pattern;
    }
}
