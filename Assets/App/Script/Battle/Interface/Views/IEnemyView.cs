namespace App.Script.Battle.Interface.Views
{
    public interface IEnemyView
    {
        uint Id { get; }
        void Init(uint id);
    }
}