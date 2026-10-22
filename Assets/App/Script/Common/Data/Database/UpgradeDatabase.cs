using System.Collections.Generic;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Database/UpgradeDatabase")]
    public class UpgradeDatabase : ScriptableObject
    {
        [SerializeField] private UpgradeMasterData[] _upgradeMasterData;

        public UpgradeMasterData[] UpgradeMasterData => _upgradeMasterData;

        // ID→マスターデータの索引。毎フレーム・毎命中で ID 引きされるため線形探索を避ける。
        // 配列の差し替え（インポート・Inspector編集）後に作り直せるよう遅延生成にする
        private Dictionary<string, UpgradeMasterData> _idIndex;

        public bool TryGetUpgradeMasterData(string id, out UpgradeMasterData upgradeMasterData)
        {
            if (string.IsNullOrEmpty(id))
            {
                upgradeMasterData = null;
                return false;
            }

            _idIndex ??= BuildIdIndex();
            return _idIndex.TryGetValue(id, out upgradeMasterData);
        }

        private Dictionary<string, UpgradeMasterData> BuildIdIndex()
        {
            var index = new Dictionary<string, UpgradeMasterData>();
            if (_upgradeMasterData == null)
            {
                return index;
            }

            foreach (var data in _upgradeMasterData)
            {
                if (data == null || string.IsNullOrEmpty(data.Id))
                {
                    continue;
                }

                // ID が重複していたら先勝ち（インポーターが重複を弾く前提。ここでは落とさない）
                if (!index.TryAdd(data.Id, data))
                {
                    Debug.LogWarning($"[UpgradeDatabase] アップグレードID \"{data.Id}\" が重複しています（{data.name}）");
                }
            }

            return index;
        }

        private void OnEnable()
        {
            _idIndex = null;
        }

        private void OnValidate()
        {
            _idIndex = null;
        }
    }
}
