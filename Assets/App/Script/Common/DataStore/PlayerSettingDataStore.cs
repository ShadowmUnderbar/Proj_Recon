using App.Common.Data;
using App.Common.Interface;
using R3;

namespace App.Script.Common.DataStore
{
    public class PlayerSettingDataStore : IPlayerSettingDataStore
    {
        public ReactiveProperty<HandType> DominantHand { get; } = new(HandType.Right);

        public ReactiveProperty<HandType> NonDominantHand => DominantHand.Value == HandType.Right
            ? new ReactiveProperty<HandType>(HandType.Left)
            : new ReactiveProperty<HandType>(HandType.Right);
    }
}