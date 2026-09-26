using System;
using System.Collections.Generic;
using App.Common.Interface;

namespace App.Common.DataStore
{
    /// <summary>
    /// 次のランで装備するアップグレードセットの選択結果を保持する。
    /// 永続化はしない。メインメニューで選び直すたびに上書きされる。
    /// </summary>
    public class RunLoadoutDataStore : IRunLoadoutDataStore
    {
        private bool _hasSelection;
        private IReadOnlyList<string> _selectedUpgradeIds = Array.Empty<string>();

        public bool HasSelection => _hasSelection;
        public IReadOnlyList<string> SelectedUpgradeIds => _selectedUpgradeIds;

        public void Select(IReadOnlyList<string> upgradeIds)
        {
            _hasSelection = true;
            // 元のスロットが後から上書き保存されても影響を受けないよう、この時点の内容を写しておく
            _selectedUpgradeIds = upgradeIds != null ? new List<string>(upgradeIds) : Array.Empty<string>();
        }

        public void SelectNone()
        {
            _hasSelection = true;
            _selectedUpgradeIds = Array.Empty<string>();
        }

        public void Clear()
        {
            _hasSelection = false;
            _selectedUpgradeIds = Array.Empty<string>();
        }
    }
}
