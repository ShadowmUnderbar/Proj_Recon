using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 所持アップグレードの効果値を種類ごとに合成して返す計算基盤。
    /// 毎フレーム・毎命中・毎射撃で呼ばれるため、所持内容が変わったとき（<see cref="IUpgradeSessionDataStore.Version"/> が進んだとき）
    /// だけ種類ごとの集計を作り直し、問い合わせは辞書引きで済ませる。
    /// 世代番号で判定するので、OnChanged の購読順（他の購読者が先に問い合わせても）に左右されない
    /// </summary>
    public class UpgradeEffectSimpleCalculatorDataStore : IUpgradeEffectSimpleCalculatorDataStore
    {
        // 種類ごとの集計結果
        private sealed class Summary
        {
            // 全レベルの Value1 の積（乗算合成用）
            public float Multiply = 1f;

            // 全レベルの Value1 の最大値（最高値のみ採用用）
            public float Max;

            // 所持中の最高レベル行
            public UpgradeMasterData Highest;
        }

        // まだ一度も集計していないことを表す世代番号（セッション側は 0 始まり）
        private const int NotBuiltVersion = -1;

        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly Dictionary<UpgradeType, Summary> _summaries = new();

        // 集計したときのセッションの世代番号
        private int _builtVersion = NotBuiltVersion;

        [Inject]
        public UpgradeEffectSimpleCalculatorDataStore(
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeDatabase = upgradeDatabase;
        }

        public float CalcMultiply(UpgradeType upgradeType)
        {
            return TryGetSummary(upgradeType, out var summary) ? summary.Multiply : 1f;
        }

        public float CalcMax(UpgradeType upgradeType)
        {
            return TryGetSummary(upgradeType, out var summary) ? summary.Max : 0f;
        }

        public int CalcMaxLevel(UpgradeType upgradeType)
        {
            return TryGetSummary(upgradeType, out var summary) ? summary.Highest.Level : 0;
        }

        public bool TryGetHighestLevelUpgrade(UpgradeType upgradeType, out UpgradeMasterData upgradeMasterData)
        {
            if (TryGetSummary(upgradeType, out var summary))
            {
                upgradeMasterData = summary.Highest;
                return true;
            }

            upgradeMasterData = null;
            return false;
        }

        private bool TryGetSummary(UpgradeType upgradeType, out Summary summary)
        {
            if (_builtVersion != _upgradeSessionDataStore.Version)
            {
                Rebuild();
            }

            return _summaries.TryGetValue(upgradeType, out summary);
        }

        private void Rebuild()
        {
            _summaries.Clear();

            foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
            {
                if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    continue;
                }

                if (!_summaries.TryGetValue(data.UpgradeType, out var summary))
                {
                    summary = new Summary();
                    _summaries.Add(data.UpgradeType, summary);
                }

                var value1 = data.Value1.value;
                summary.Multiply *= value1;
                if (value1 > summary.Max)
                {
                    summary.Max = value1;
                }

                if (summary.Highest == null || data.Level > summary.Highest.Level)
                {
                    summary.Highest = data;
                }
            }

            _builtVersion = _upgradeSessionDataStore.Version;
        }
    }
}
