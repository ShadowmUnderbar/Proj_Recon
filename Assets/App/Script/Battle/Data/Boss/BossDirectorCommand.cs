namespace App.Battle.Data
{
    public enum BossDirectorCommandType
    {
        /// <summary>指定の行動を始めさせる</summary>
        Act,

        /// <summary>その場に留めて待機させる（移動も止める）</summary>
        Hold,

        /// <summary>待機を解除する</summary>
        Release
    }

    /// <summary>
    /// ボスの台本進行が個体へ出す命令。DataStoreが生成し、UseCaseがPresenter経由で個体へ届ける。
    /// </summary>
    public readonly struct BossDirectorCommand
    {
        private BossDirectorCommand(BossDirectorCommandType type, int enemyId, int actionIndex)
        {
            Type = type;
            EnemyId = enemyId;
            ActionIndex = actionIndex;
        }

        public BossDirectorCommandType Type { get; }
        public int EnemyId { get; }

        /// <summary>Act のときの行動番号（ボスAIごとに意味が決まる）</summary>
        public int ActionIndex { get; }

        public static BossDirectorCommand Act(int enemyId, int actionIndex)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Act, enemyId, actionIndex);
        }

        public static BossDirectorCommand Hold(int enemyId)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Hold, enemyId, 0);
        }

        public static BossDirectorCommand Release(int enemyId)
        {
            return new BossDirectorCommand(BossDirectorCommandType.Release, enemyId, 0);
        }

        public override string ToString()
        {
            return Type == BossDirectorCommandType.Act
                ? $"{Type}(id:{EnemyId}, action:{ActionIndex})"
                : $"{Type}(id:{EnemyId})";
        }
    }
}
