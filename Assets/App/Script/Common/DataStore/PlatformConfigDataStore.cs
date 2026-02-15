using App.Common.Data;
using App.Common.Interface;

namespace App.Common.DataStore
{
    /// <summary>
    /// DebugConfigの値をDI経由で提供するDataStore
    /// </summary>
    public class PlatformConfigDataStore : IPlatformConfigDataStore
    {
        public bool IsVRMode { get; } = DebugConfig.IsVRMode;
        public bool IsAllUnLock { get; } = DebugConfig.IsAllUnLock;
    }
}
