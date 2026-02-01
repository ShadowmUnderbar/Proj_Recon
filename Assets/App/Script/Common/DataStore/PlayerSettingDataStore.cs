using App.Common.Data;
using App.Common.Interface;
using R3;

namespace App.Common.DataStore
{
    public class PlayerSettingDataStore : IPlayerSettingDataStore
    {
        public ReactiveProperty<HandType> DominantHand { get; } = new(HandType.Right);

        public HandType NonDominantHand => DominantHand.Value == HandType.Right ? HandType.Left : HandType.Right;
    }
}