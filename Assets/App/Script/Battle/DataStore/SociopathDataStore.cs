using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ソシオパス: 敵を撃破すると、レイジを Value1 秒得る（発動していなければ発動する）。
    /// すでにレイジが発動中なら Value1 × Value2 秒を得る。
    /// レイジ未所持でも、BuffId のレイジを被弾では延びない状態で発動させる。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class SociopathDataStore : ISociopathDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly BuffDatabase _buffDatabase;

        [Inject]
        public SociopathDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore,
            BuffDatabase buffDatabase
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
            _buffDatabase = buffDatabase;
        }

        public float NotifyKill()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Sociopath, out var upgrade))
            {
                return 0f;
            }

            // 発動中かは獲得前に判定する（獲得で発動した直後を「発動中」と数えない）
            var seconds = Mathf.Max(0f, upgrade.Value1.value);
            if (_buffStateDataStore.IsRageActive)
            {
                seconds *= Mathf.Max(0f, upgrade.Value2.value);
            }

            _buffStateDataStore.GainRage(seconds, GetRageWhenNotOwned(upgrade));
            return seconds;
        }

        // レイジ未所持のときに発動させるレイジ（ソシオパス行の BuffId）
        private BuffMasterData GetRageWhenNotOwned(UpgradeMasterData upgrade)
        {
            if (_buffDatabase.TryGetBuffMasterData(upgrade.BuffId, out var buff))
            {
                return buff;
            }

            Debug.LogWarning($"[SociopathDataStore] BuffId \"{upgrade.BuffId}\" が BuffDatabase に見つかりません (Upgrade: {upgrade.Id})");
            return null;
        }
    }
}
