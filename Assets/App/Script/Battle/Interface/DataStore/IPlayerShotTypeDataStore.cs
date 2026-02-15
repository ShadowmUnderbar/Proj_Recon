using App.Common.Data;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerShotTypeDataStore
    {
        ReactiveProperty<ShotType> ShotType { get; }
    }
}
