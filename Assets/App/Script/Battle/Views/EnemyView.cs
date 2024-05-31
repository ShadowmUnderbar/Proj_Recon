using App.Script.Battle.Interface.Views;

namespace App.Script.Battle.Views
{
    public class EnemyView : IEnemyView
    {
        private uint _id;

        public void Init(uint id)
        {
            _id = id;
        }
    }
}