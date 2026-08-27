namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤーが受けた攻撃1発分の情報。
    /// ダメージ量だけでなく「誰の攻撃か」「弾か近接か」を持つため、
    /// 回避中に接触した敵弾・敵を数えて跳ね返す（回避時跳ね返し攻撃）といった処理に使える。
    /// </summary>
    public readonly struct PlayerDamagedData
    {
        public PlayerDamagedData(float damage, int attackerId, bool isProjectile, int projectileId = 0)
        {
            Damage = damage;
            AttackerId = attackerId;
            IsProjectile = isProjectile;
            ProjectileId = projectileId;
        }

        /// <summary>受けたダメージ量（軽減・無効化の前）</summary>
        public float Damage { get; }

        /// <summary>攻撃してきた敵のId</summary>
        public int AttackerId { get; }

        /// <summary>弾による攻撃か（false は近接攻撃・爆風）</summary>
        public bool IsProjectile { get; }

        /// <summary>
        /// この攻撃を発生させた弾の一意なId（弾以外は0）。
        /// 同一の弾を重複してカウントしないための識別に使う
        /// </summary>
        public int ProjectileId { get; }
    }
}
