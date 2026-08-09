using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 依存ノード（α・β・γ）の実行時状態。
    /// 単体では効果を持たず、ダメージ・ノードやケア・ノードが参照する「有効な種類数」を提供する。
    /// 無効化はこのランの実行時状態としてのみ保持するため、獲得済みアップグレードの保存内容には影響しない
    /// （次のランでは再び有効な状態で読み込まれる）。
    /// </summary>
    public class DependencyNodeDataStore : IDependencyNodeDataStore
    {
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeDatabase _upgradeDatabase;

        // 無効化済みの依存ノードID
        private readonly HashSet<string> _disabledIds = new();

        [Inject]
        public DependencyNodeDataStore(
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeDatabase = upgradeDatabase;
        }

        public int ActiveNodeCount
        {
            get
            {
                var count = 0;
                foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
                {
                    if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                    {
                        continue;
                    }

                    if (data.UpgradeType != UpgradeType.DependencyNode)
                    {
                        continue;
                    }

                    if (_disabledIds.Contains(id))
                    {
                        continue;
                    }

                    count++;
                }

                return count;
            }
        }

        public bool TryDisableOne(out string disabledNameKey)
        {
            // マスターデータの並び順（α→β→γ）で先頭の有効なノードを無効化する
            foreach (var data in _upgradeDatabase.UpgradeMasterData)
            {
                if (data.UpgradeType != UpgradeType.DependencyNode)
                {
                    continue;
                }

                if (_disabledIds.Contains(data.Id))
                {
                    continue;
                }

                if (!_upgradeSessionDataStore.AppliedUpgrades.Contains(data.Id))
                {
                    continue;
                }

                _disabledIds.Add(data.Id);
                disabledNameKey = data.NameKey;
                return true;
            }

            disabledNameKey = null;
            return false;
        }

        public bool IsDisabled(string upgradeId)
        {
            return _disabledIds.Contains(upgradeId);
        }
    }
}
