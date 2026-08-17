using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;

namespace App.Battle.UseCase
{
    /// <summary>
    /// アップグレードの付与副作用（GrantBuffのバフ起動・バリアの満タン付与）を適用する共通処理。
    /// ショップでの新規獲得（<see cref="ShopUseCase"/>）とセット読込（<see cref="RunStartUseCase"/>）の
    /// 両方から使う。セッションへの追加（AddUpgrade/Preload）は呼び出し側の責務。
    /// </summary>
    public class UpgradeSideEffectApplier
    {
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IPlayerBarrierDataStore _playerBarrierDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly BuffDatabase _buffDatabase;

        [Inject]
        public UpgradeSideEffectApplier(
            IBuffStateDataStore buffStateDataStore,
            IPlayerBarrierDataStore playerBarrierDataStore,
            IPlayerStateDataStore playerStateDataStore,
            BuffDatabase buffDatabase
        )
        {
            _buffStateDataStore = buffStateDataStore;
            _playerBarrierDataStore = playerBarrierDataStore;
            _playerStateDataStore = playerStateDataStore;
            _buffDatabase = buffDatabase;
        }

        public void Apply(UpgradeMasterData upgrade)
        {
            // バフ付与型: 対応するバフの監視を開始する
            if (upgrade.UpgradeType == UpgradeType.GrantBuff)
            {
                if (_buffDatabase.TryGetBuffMasterData(upgrade.BuffId, out var buffMasterData))
                {
                    _buffStateDataStore.AddBuff(buffMasterData);
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSideEffectApplier] BuffId \"{upgrade.BuffId}\" が BuffDatabase に見つかりません (Upgrade: {upgrade.Id})");
                }
            }

            // HP最大値型: 最大HPを再計算する（増えた分は現在HPにも入る）。
            // バリア付与より先に行い、強化後の最大HPを基準にバリアが張られるようにする
            if (upgrade.UpgradeType == UpgradeType.Health)
            {
                _playerStateDataStore.RefreshMaxHealth();
            }

            // バリア型: 最大HP×倍率で満タン付与
            if (upgrade.UpgradeType == UpgradeType.Barrier)
            {
                _playerBarrierDataStore.GrantFull(_playerStateDataStore.MaxHealth.Value);
            }
        }
    }
}
