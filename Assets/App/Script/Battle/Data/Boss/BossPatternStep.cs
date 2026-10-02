using System;
using UnityEngine;

namespace App.Battle.Data
{
    public enum BossPatternStepType
    {
        /// <summary>
        /// 対象の個体が全員行動可能になるのを待ち、同じフレームで一斉に行動を始めさせる。
        /// </summary>
        Act,

        /// <summary>対象の個体が全員行動可能（行動・硬直・スタンが明けた状態）になるまで待つ</summary>
        WaitActionable,

        /// <summary>指定秒数待つ（フリーズ・ウェーブ間ポーズ中は進まない）</summary>
        Wait,

        /// <summary>
        /// 対象をプレイヤーの縦方向（上下）と横方向（左右）に交互に振り分けて配置し直す。
        /// どの個体がどちらになるか・正負のどちら側かはランダム。同じ軸に偏らない（2体なら必ず縦と横の組）
        /// </summary>
        CrossFormation
    }

    /// <summary>
    /// ボスの行動台本の1ステップ。個体は <see cref="BossGroupConfig"/> のメンバー番号（スロット）で指定する。
    /// 撃破済みの個体は対象から外して扱い、対象が全員撃破済みのステップは即座に飛ばす。
    /// </summary>
    [Serializable]
    public class BossPatternStep
    {
        [SerializeField] private BossPatternStepType _type;

        [SerializeField, Tooltip("対象のメンバー番号（BossGroupConfigのメンバー配列の添字）。Wait では使わない")]
        private int[] _memberSlots = Array.Empty<int>();

        [SerializeField, Min(0), Tooltip("Act で行わせる行動の番号（ボスAIごとに意味が決まる）")]
        private int _actionIndex;

        [SerializeField, Tooltip("Act のみ。対象が行動・硬直を終えるまで、対象以外のメンバーをその場で待機させ、このステップに留まる")]
        private bool _holdOthers;

        [SerializeField, Min(0f), Tooltip("Wait で待つ秒数")]
        private float _waitSeconds;

        [SerializeField, Tooltip("CrossFormation のみ。配置先を横（移動方向と直交する向き）へずらす量の候補（m）。個体ごとにランダムに選ぶ。空ならずらさない")]
        private float[] _lateralOffsets = System.Array.Empty<float>();

        [SerializeField, Tooltip("CrossFormation のみ。少なくとも1体はずれ0（プレイヤーと重なる位置）にする。候補に0が無いときは効かない")]
        private bool _requireAlignedOne;

        public BossPatternStepType Type => _type;
        public int[] MemberSlots => _memberSlots;
        public int ActionIndex => _actionIndex;
        public bool HoldOthers => _holdOthers;
        public float WaitSeconds => _waitSeconds;
        public float[] LateralOffsets => _lateralOffsets;
        public bool RequireAlignedOne => _requireAlignedOne;
    }
}
