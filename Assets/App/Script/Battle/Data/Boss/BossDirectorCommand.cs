namespace App.Battle.Data
{
    public enum BossDirectorCommandType
    {
        /// <summary>指定の行動を始めさせる</summary>
        Act,

        /// <summary>その場に留めて待機させる（移動も止める）</summary>
        Hold,

        /// <summary>待機を解除する</summary>
        Release,

        /// <summary>指定の位置（プレイヤーの上下左右）へ配置し直す</summary>
        Formation,

        /// <summary>行動を打ち切って待機へ戻す（発狂フェイズへの切り替えなど）</summary>
        Cancel,

        /// <summary>時止めを始める（個体ではなく場全体への命令。EnemyId は使わない）</summary>
        BeginTimeStop,

        /// <summary>時止めを解く（個体ではなく場全体への命令。EnemyId は使わない）</summary>
        EndTimeStop
    }

    /// <summary>
    /// ボスの台本進行が個体へ出す命令。DataStoreが生成し、UseCaseがPresenter経由で個体へ届ける。
    /// </summary>
    public readonly struct BossDirectorCommand
    {
        private BossDirectorCommand(BossDirectorCommandType type, int enemyId, int actionIndex,
            BossFormationSlot slot = BossFormationSlot.None, float lateralOffset = 0f,
            BossTurnDirection turnDirection = BossTurnDirection.None, float moveSeconds = 0f)
        {
            Type = type;
            EnemyId = enemyId;
            ActionIndex = actionIndex;
            Slot = slot;
            LateralOffset = lateralOffset;
            TurnDirection = turnDirection;
            MoveSeconds = moveSeconds;
        }

        // 場全体への命令（時止め）で EnemyId に入れる値
        private const int NoEnemyId = -1;

        public BossDirectorCommandType Type { get; }
        public int EnemyId { get; }

        /// <summary>Act のときの行動番号（ボスAIごとに意味が決まる）</summary>
        public int ActionIndex { get; }

        /// <summary>Formation のときの配置先</summary>
        public BossFormationSlot Slot { get; }

        /// <summary>Formation のときの横（移動方向と直交する向き）へのずれ（m）</summary>
        public float LateralOffset { get; }

        /// <summary>Act のときの回りこむ向き（回りこむ行動でだけ使う）</summary>
        public BossTurnDirection TurnDirection { get; }

        /// <summary>Formation のときに配置先へ移動するのに掛ける秒数（0なら瞬間移動）</summary>
        public float MoveSeconds { get; }

        public static BossDirectorCommand Act(int enemyId, int actionIndex,
            BossTurnDirection turnDirection = BossTurnDirection.None)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Act, enemyId, actionIndex,
                turnDirection: turnDirection);
        }

        public static BossDirectorCommand Hold(int enemyId)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Hold, enemyId, 0);
        }

        public static BossDirectorCommand Release(int enemyId)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Release, enemyId, 0);
        }

        public static BossDirectorCommand Formation(int enemyId, BossFormationSlot slot, float lateralOffset = 0f,
            float moveSeconds = 0f)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Formation, enemyId, 0, slot, lateralOffset,
                moveSeconds: moveSeconds);
        }

        public static BossDirectorCommand Cancel(int enemyId)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Cancel, enemyId, 0);
        }

        public static BossDirectorCommand BeginTimeStop()
        {
            return new BossDirectorCommand(BossDirectorCommandType.BeginTimeStop, NoEnemyId, 0);
        }

        public static BossDirectorCommand EndTimeStop()
        {
            return new BossDirectorCommand(BossDirectorCommandType.EndTimeStop, NoEnemyId, 0);
        }

        public override string ToString()
        {
            return Type switch
            {
                BossDirectorCommandType.Act when TurnDirection != BossTurnDirection.None =>
                    $"{Type}(id:{EnemyId}, action:{ActionIndex}, turn:{TurnDirection})",
                BossDirectorCommandType.Act => $"{Type}(id:{EnemyId}, action:{ActionIndex})",
                BossDirectorCommandType.Formation => $"{Type}(id:{EnemyId}, slot:{Slot}, offset:{LateralOffset}, move:{MoveSeconds})",
                BossDirectorCommandType.BeginTimeStop or BossDirectorCommandType.EndTimeStop => Type.ToString(),
                _ => $"{Type}(id:{EnemyId})"
            };
        }
    }
}
