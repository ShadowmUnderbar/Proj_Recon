namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤーが受けた攻撃1発分の情報。
    /// ダメージ量だけでなく「誰の攻撃か」「弾か近接か」を持つため、
    /// 回避中の被弾を撃ってきた相手に跳ね返す（パリングダガー）といった処理に使える。
    /// </summary>
    public readonly struct PlayerDamagedData
    {
        public PlayerDamagedData(float damage, int attackerId, bool isProjectile)
        {
            Damage = damage;
            AttackerId = attackerId;
            IsProjectile = isProjectile;
        }

        /// <summary>受けたダメージ量（軽減・無効化の前）</summary>
        public float Damage { get; }

        /// <summary>攻撃してきた敵のId</summary>
        public int AttackerId { get; }

        /// <summary>弾による攻撃か（false は近接攻撃・爆風）</summary>
        public bool IsProjectile { get; }
    }
}
