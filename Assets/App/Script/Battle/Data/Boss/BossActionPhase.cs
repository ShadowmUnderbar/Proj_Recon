namespace App.Battle.Data
{
    /// <summary>
    /// ボスの1回の行動の進行段階。
    /// Ready 以外の間は次の行動を受け付けない（攻撃後の隙もここに含む）。
    /// </summary>
    public enum BossActionPhase
    {
        /// <summary>行動していない（命令を受け付けられる）</summary>
        Ready,

        /// <summary>予備動作</summary>
        Windup,

        /// <summary>攻撃の持続（開始時に攻撃を出す）</summary>
        Active,

        /// <summary>攻撃後の硬直</summary>
        Recovery
    }
}
