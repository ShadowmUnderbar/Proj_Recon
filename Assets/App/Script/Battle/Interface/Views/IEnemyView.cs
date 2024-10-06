namespace App.Battle.Interface
{
    public interface IEnemyView
    {
        uint Id { get; }
        void Init(uint id);
    }
}