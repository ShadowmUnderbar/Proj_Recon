namespace App.Battle.Data
{
    /// <summary>
    /// ウイルスによって1回（1秒ぶん）に与えるダメージ
    /// </summary>
    public readonly struct VirusDamage
    {
        public VirusDamage(int enemyId, float damage)
        {
            EnemyId = enemyId;
            Damage = damage;
        }

        public int EnemyId { get; }

        public float Damage { get; }
    }
}
