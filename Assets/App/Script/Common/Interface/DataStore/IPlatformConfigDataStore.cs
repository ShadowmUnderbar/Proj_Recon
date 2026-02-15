namespace App.Common.Interface
{
    /// <summary>
    /// プラットフォーム設定（VRモード等）へのアクセスを提供する
    /// DebugConfigの直接参照を避け、DIによるテスト容易性を確保する
    /// </summary>
    public interface IPlatformConfigDataStore
    {
        bool IsVRMode { get; }
        bool IsAllUnLock { get; }
    }
}
