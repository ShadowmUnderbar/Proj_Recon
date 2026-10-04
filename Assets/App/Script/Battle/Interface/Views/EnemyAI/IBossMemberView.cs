using App.Battle.Data;
using R3;

namespace App.Battle.Interface.EnemyAI
{
    /// <summary>
    /// ボスグループの台本から命令を受けて動く個体。
    /// 自分では攻撃を始めず、<see cref="CommandAction"/> で命令されたときだけ行動する。
    /// </summary>
    public interface IBossMemberView
    {
        /// <summary>行動可否の判断に使う状態（行動段階・スタン・撃破）</summary>
        ReadOnlyReactiveProperty<BossMemberStatus> Status { get; }

        /// <summary>
        /// 指定の行動を始める。行動不能（行動中・硬直・スタン・撃破済み）なら何もせず false を返す。
        /// turnDirection は回りこむ行動での回る向き（それ以外の行動では None）
        /// </summary>
        bool CommandAction(int actionIndex, BossTurnDirection turnDirection);

        /// <summary>その場での待機を設定する（待機中は移動も止める）</summary>
        void SetHold(bool isHold);

        /// <summary>
        /// プレイヤーに対してつく位置を指定する（対応するボスAIは指定位置へ瞬間移動する）。
        /// lateralOffset は配置先を横（プレイヤーへ向かう向きと直交する向き）へずらす量（m）
        /// </summary>
        void SetFormation(BossFormationSlot slot, float lateralOffset);

        /// <summary>行動を打ち切って待機へ戻す（行動していなければ何もしない）</summary>
        void CancelAction();
    }
}
