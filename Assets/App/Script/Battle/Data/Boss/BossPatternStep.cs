using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        /// 時止めの回りこみ攻撃。全員の行動が明けたら時を止め（プレイヤーの移動・攻撃・回避と弾が止まる）、
        /// 対象の全員に、プレイヤーを中心に同じ向き（時計回りか反時計回りかはランダム）へ90度回りこませる。
        /// 対象からランダムに選んだ1体は ActionIndex（回りこみながら連射）、残りは PartnerActionIndex（回りこむだけ）を行う。
        /// 全員が回りこみ終えたら時止めを解き（止めていた弾が動き出す）、WaitSeconds 秒おいてから次へ進む。
        /// 対象以外のメンバーは実行中ずっと、対象も時止めを解いてからの一息の間はその場で待機させる
        /// </summary>
        TimeStopOrbit
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

        [SerializeField, Min(0), Tooltip("Act で行わせる行動の番号（ボスAIごとに意味が決まる）。TimeStopOrbit では回りこみながら連射する行動の番号")]
        private int _actionIndex;

        [SerializeField, Tooltip("Act のみ。対象が行動・硬直を終えるまで、対象以外のメンバーをその場で待機させ、このステップに留まる")]
        private bool _holdOthers;

        [SerializeField, Min(0f), Tooltip("Wait で待つ秒数。TimeStopOrbit では時止めを解いてから次のステップへ進むまでの秒数")]
        private float _waitSeconds;

        [SerializeField, Tooltip("CrossFormation のみ。配置先を横（移動方向と直交する向き）へずらす量の候補（m）。個体ごとにランダムに選ぶ。空ならずらさない")]
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
        [SerializeField, Min(0), FormerlySerializedAs("_replayActionIndex"),
         Tooltip("TimeStopOrbit のみ。連射しない個体が行う、回りこむだけの行動の番号")]
        private int _partnerActionIndex;

        public bool HoldOthers => _holdOthers;
        public float WaitSeconds => _waitSeconds;
        public float[] LateralOffsets => _lateralOffsets;
        public bool RequireAlignedOne => _requireAlignedOne;
        public int LoopBackSteps => _loopBackSteps;
        public int LoopMin => _loopMin;
        public int LoopMax => _loopMax;
        public int PartnerActionIndex => _partnerActionIndex;
    }
}
