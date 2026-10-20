namespace App.Battle.Data
{
    /// <summary>
    /// ウイルスを付与した結果（発生か重複か、付与の前後の状態）
    /// </summary>
    public readonly struct VirusApplyResult
    {
        public VirusApplyResult(int enemyId, float addedPower, float addedDuration, bool isNew, VirusStatus before,
            VirusStatus after, bool isDurationReplaced, float damagePerSecond)
        {
            EnemyId = enemyId;
            AddedPower = addedPower;
            AddedDuration = addedDuration;
            IsNew = isNew;
            Before = before;
            After = after;
            IsDurationReplaced = isDurationReplaced;
            DamagePerSecond = damagePerSecond;
        }

        public int EnemyId { get; }

        /// <summary>付与した威力</summary>
        public float AddedPower { get; }

        /// <summary>付与した効果時間（秒）</summary>
        public float AddedDuration { get; }

        /// <summary>ウイルスが付いていない敵への付与（発生）なら true、すでに付いていた（重複）なら false</summary>
        public bool IsNew { get; }

        /// <summary>付与前の状態（発生時は既定値）</summary>
        public VirusStatus Before { get; }

        /// <summary>付与後の状態</summary>
        public VirusStatus After { get; }

        /// <summary>付与後の1秒ごとのダメージ（1以下は整数に四捨五入した値）</summary>
        public float DamagePerSecond { get; }

        /// <summary>重複で効果時間（最長・残り）が新しい値に置き換わったか</summary>
        public bool IsDurationReplaced { get; }
    }
}
