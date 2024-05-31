using App.Script.Common.Data.MasterData;

namespace App.Script.Battle.Data
{
    public class EnemyData
    {
        public uint Id { get; set; }
        public EnemyMasterData MasterData { get; set; }
        public float Hp { get; set; }
        public float Damage { get; set; }
        public float Speed { get; set; }
    }
}