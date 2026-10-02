using System.Collections.Generic;
using App.Battle.Data;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 台本の TimeStopMemory ステップの進み具合と、時止めの間に見せた予兆の記録。
    /// 進め方は <see cref="BossPatternRunner"/> が決め、ここは状態だけを持つ。
    /// </summary>
    public class BossTimeStopMemory
    {
        /// <summary>ステップ全体の段階</summary>
        public enum Stage
        {
            /// <summary>始まっていない（全員の行動が明けるのを待つ）</summary>
            None,

            /// <summary>時を止めて予兆を順に見せている</summary>
            Telegraph,

            /// <summary>時止めを解いて、攻撃を始めるまでの一息</summary>
            Breath,

            /// <summary>見せた予兆どおりに攻撃している</summary>
            Replay
        }

        /// <summary>予兆1回・攻撃1回の中の段階</summary>
        public enum Turn
        {
            /// <summary>配置を出す</summary>
            Position,

            /// <summary>配置についた個体へ行動を命令する</summary>
            Act,

            /// <summary>行動が終わるのを待つ</summary>
            Await
        }

        /// <summary>見せた予兆1回ぶん（どの個体が・プレイヤーのどちら側から・横へどれだけずれて）</summary>
        public readonly struct Record
        {
            public Record(int memberSlot, BossFormationSlot formation, float lateralOffset)
            {
                MemberSlot = memberSlot;
                Formation = formation;
                LateralOffset = lateralOffset;
            }

            /// <summary>メンバー番号（BossGroupConfig のメンバー配列の添字）</summary>
            public int MemberSlot { get; }

            public BossFormationSlot Formation { get; }
            public float LateralOffset { get; }
        }

        private readonly List<Record> _records = new();

        public Stage CurrentStage { get; private set; } = Stage.None;
        public Turn CurrentTurn { get; set; } = Turn.Position;

        /// <summary>Replay で次に再現する記録の番号</summary>
        public int ReplayIndex { get; set; }

        /// <summary>Breath の経過秒数</summary>
        public float BreathElapsed { get; set; }

        public IReadOnlyList<Record> Records => _records;

        /// <summary>時を止めている間か（予兆を見せている段階）</summary>
        public bool IsTimeStopping => CurrentStage == Stage.Telegraph;

        /// <summary>直近の記録（予兆を見せている個体）。記録が無ければ呼ばないこと</summary>
        public Record Current => _records[^1];

        /// <summary>Replay で再現している記録</summary>
        public Record ReplayRecord => _records[ReplayIndex];

        public void BeginTelegraph()
        {
            _records.Clear();
            CurrentStage = Stage.Telegraph;
            CurrentTurn = Turn.Position;
        }

        public void AddRecord(Record record)
        {
            _records.Add(record);
        }

        public void BeginBreath()
        {
            CurrentStage = Stage.Breath;
            BreathElapsed = 0f;
        }

        public void BeginReplay()
        {
            CurrentStage = Stage.Replay;
            CurrentTurn = Turn.Position;
            ReplayIndex = 0;
        }

        public void Reset()
        {
            _records.Clear();
            CurrentStage = Stage.None;
            CurrentTurn = Turn.Position;
            ReplayIndex = 0;
            BreathElapsed = 0f;
        }
    }
}
