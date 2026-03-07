namespace App.Common.Interface
{
    public interface ICoreSkillUnlockDataStore
    {
        bool IsUnLockAkimbo { get; }
        bool IsUnLockFocus { get; }
        bool IsUnLockWaltz { get; }
        bool IsUnLockMerge { get; }
        bool IsUnLockBlitz { get; }
    }
}