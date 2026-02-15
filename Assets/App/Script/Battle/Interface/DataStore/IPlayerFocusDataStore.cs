using App.Common.Data;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerFocusDataStore
    {
        ReactiveProperty<int> FocusLeftTargetId { get; }
        ReactiveProperty<int> FocusRightTargetId { get; }
        ReactiveProperty<AimFocusType> LeftFocusType { get; }
        ReactiveProperty<AimFocusType> RightFocusType { get; }

        bool IsFocusInput { get; set; }
        ReactiveProperty<bool> IsLeftFocusInput { get; }
        ReactiveProperty<bool> IsRightFocusInput { get; }
    }
}
