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
        CrossFormation,

        /// <summary>
        /// 直前の LoopBackSteps 個のステップを、合計 LoopMin〜LoopMax 回（毎回ランダム）繰り返す。
        /// 入れ子にはできない（ループの中に別の RandomLoop を置かない）
        /// </summary>
        RandomLoop,

        /// <summary>
        /// 対象2体を、プレイヤーから見て隣り合う斜めの角（左上と右上など）へ配置する。
        /// 2体の向きが直交するので、プレイヤーへ向けた帯が×字に交わる。どの角の組になるかはランダム
        /// </summary>
        DiagonalFormation,

        /// <summary>
        /// 時止めの記憶攻撃。時を止め（プレイヤーの移動・攻撃・回避と弾が止まる）、その間に対象から1体ずつランダムに選んで
        /// プレイヤーの上下左右のどこかへ配置し、攻撃の予兆だけを MemoryCount 回、順に見せる。
        /// 見せ終えたら時止めを解き、WaitSeconds 秒おいてから、見せたときと同じ個体・同じ配置（プレイヤーの今の位置が基準）で
        /// 同じ順に ReplayActionIndex の攻撃を行う。実行中は対象以外も含めて全員をその場で待機させる
        /// </summary>
        TimeStopMemory
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

        [SerializeField, Min(0), Tooltip("Act で行わせる行動の番号（ボスAIごとに意味が決まる）。TimeStopMemory では予兆だけを見せる行動の番号")]
        private int _actionIndex;

        [SerializeField, Tooltip("Act のみ。対象が行動・硬直を終えるまで、対象以外のメンバーをその場で待機させ、このステップに留まる")]
        private bool _holdOthers;

        [SerializeField, Min(0f), Tooltip("Wait で待つ秒数。TimeStopMemory では時止めを解いてから攻撃を始めるまでの秒数")]
        private float _waitSeconds;

        [SerializeField, Tooltip("CrossFormation / TimeStopMemory。配置先を横（移動方向と直交する向き）へずらす量の候補（m）。個体ごとにランダムに選ぶ。空ならずらさない")]
        private float[] _lateralOffsets = System.Array.Empty<float>();

        [SerializeField, Tooltip("CrossFormation のみ。少なくとも1体はずれ0（プレイヤーと重なる位置）にする。候補に0が無いときは効かない")]
        private bool _requireAlignedOne;

        [SerializeField, Min(1), Tooltip("RandomLoop のみ。繰り返す範囲（このステップの直前の何個か）")]
        private int _loopBackSteps = 1;

        [SerializeField, Min(1), Tooltip("RandomLoop のみ。繰り返す回数の下限（1回目を含む合計）")]
        private int _loopMin = 1;

        [SerializeField, Min(1), Tooltip("RandomLoop のみ。繰り返す回数の上限（1回目を含む合計）")]
        private int _loopMax = 1;

        public BossPatternStepType Type => _type;
        public int[] MemberSlots => _memberSlots;
        public int ActionIndex => _actionIndex;
        [SerializeField, Min(1), Tooltip("TimeStopMemory のみ。時止めの間に見せる予兆の回数")]
        private int _memoryCount = 3;

        [SerializeField, Min(0), Tooltip("TimeStopMemory のみ。時止めを解いたあと、見せた予兆どおりに行う攻撃の行動番号")]
        private int _replayActionIndex;

        public bool HoldOthers => _holdOthers;
        public float WaitSeconds => _waitSeconds;
        public float[] LateralOffsets => _lateralOffsets;
        public bool RequireAlignedOne => _requireAlignedOne;
        public int LoopBackSteps => _loopBackSteps;
        public int LoopMin => _loopMin;
        public int LoopMax => _loopMax;
        public int MemoryCount => _memoryCount;
        public int ReplayActionIndex => _replayActionIndex;
    }
}
