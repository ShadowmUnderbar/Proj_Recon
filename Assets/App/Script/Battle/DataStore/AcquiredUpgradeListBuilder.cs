using System.Collections.Generic;
using System.Text;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    public class AcquiredUpgradeListBuilder : IAcquiredUpgradeListBuilder
    {
        private const string ItemPrefix = "・";
        // 半角スペースで区切る（旧 Text は全角スペースで折り返さないため）。名前の途中に空白は無いので、折り返しは区切りの位置で起きる
        private const string Separator = "    ";
        private const string EmptyText = "なし";

        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly IUpgradeLocalizationDataStore _upgradeLocalizationDataStore;

        [Inject]
        public AcquiredUpgradeListBuilder(
            UpgradeDatabase upgradeDatabase,
            IUpgradeLocalizationDataStore upgradeLocalizationDataStore
        )
        {
            _upgradeDatabase = upgradeDatabase;
            _upgradeLocalizationDataStore = upgradeLocalizationDataStore;
        }

        public string Build(IReadOnlyList<string> upgradeIds)
        {
            if (upgradeIds == null || upgradeIds.Count == 0)
            {
                return EmptyText;
            }

            var sb = new StringBuilder();
            foreach (var id in upgradeIds)
            {
                if (sb.Length > 0)
                {
                    sb.Append(Separator);
                }

                sb.Append(ItemPrefix).Append(GetDisplayName(id));
            }

            return sb.ToString();
        }

        private string GetDisplayName(string id)
        {
            // マスターデータから消えたIDはセーブ由来などで起こりうる。一覧から落とさずIDのまま出して気付けるようにする
            if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var upgrade))
            {
                Debug.LogWarning($"[AcquiredUpgradeListBuilder] アップグレード {id} がマスターデータにありません");
                return id;
            }

            var text = _upgradeLocalizationDataStore.GetText(upgrade);
            return text.Title + text.LevelLabel;
        }
    }
}
