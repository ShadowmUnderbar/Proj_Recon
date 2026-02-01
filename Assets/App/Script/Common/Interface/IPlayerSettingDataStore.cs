using App.Common.Data;
using R3;

namespace App.Common.Interface
{
    public interface IPlayerSettingDataStore
    {
        ReactiveProperty<HandType> DominantHand { get; }
        HandType NonDominantHand { get; }
    }
}