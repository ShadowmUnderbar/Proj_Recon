using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// アンガーマネジメント: 被弾すると、Value2 秒後に受けたダメージ × Value1 秒のレイジを得る（発動していなければ発動する）。
    /// 秒数・待ち時間は被弾した時点の最高レベルの値で決める。
    /// レイジ未所持でも、BuffId のレイジを被弾では延びない状態で発動させる。
    /// オーバークロック中・ウェーブ間ポーズ（ショップ等）の間はレイジと同じく待ち時間を進めない
    /// </summary>
    public class AngerManagementDataStore : IAngerManagementDataStore, ITickable, IRunResettable
    {
        private struct PendingRage
        {
            // レイジを得るまでの残り秒数
            public float RemainingDelay;

            // 得るレイジの秒数
            public float Seconds;

            // レイジ未所持のときに発動させるレイジ
            public BuffMasterData RageWhenNotOwned;
        }

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IOverclockDataStore _overclockDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly BuffDatabase _buffDatabase;

        private readonly List<PendingRage> _pendingRages = new();

        [Inject]
        public AngerManagementDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore,
            IOverclockDataStore overclockDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            BuffDatabase buffDatabase
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
            _overclockDataStore = overclockDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _buffDatabase = buffDatabase;
        }

        public float NotifyDamaged(float damage)
        {
            if (damage <= 0f ||
                !_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.AngerManagement, out var upgrade))
            {
                return 0f;
            }

            var seconds = damage * Mathf.Max(0f, upgrade.Value1.value);
            if (seconds <= 0f)
            {
                return 0f;
            }

            _pendingRages.Add(new PendingRage
            {
                RemainingDelay = Mathf.Max(0f, upgrade.Value2.value),
                Seconds = seconds,
                RageWhenNotOwned = GetRageWhenNotOwned(upgrade)
            });
            return seconds;
        }

        public void Tick()
        {
            // オーバークロック中・ウェーブ間ポーズ中はレイジの残り時間と同じく待ち時間も止める
            if (_pendingRages.Count == 0 ||
                _overclockDataStore.IsActive.CurrentValue ||
                _waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            for (var i = _pendingRages.Count - 1; i >= 0; i--)
            {
                var pending = _pendingRages[i];
                pending.RemainingDelay -= Time.deltaTime;
                if (pending.RemainingDelay > 0f)
                {
                    _pendingRages[i] = pending;
                    continue;
                }

                _pendingRages.RemoveAt(i);
                _buffStateDataStore.GainRage(pending.Seconds, pending.RageWhenNotOwned);
            }
        }

        // レイジ未所持のときに発動させるレイジ（アンガーマネジメント行の BuffId）
        private BuffMasterData GetRageWhenNotOwned(UpgradeMasterData upgrade)
        {
            if (_buffDatabase.TryGetBuffMasterData(upgrade.BuffId, out var buff))
            {
                return buff;
            }

            Debug.LogWarning($"[AngerManagementDataStore] BuffId \"{upgrade.BuffId}\" が BuffDatabase に見つかりません (Upgrade: {upgrade.Id})");
            return null;
        }

        public void ResetRun()
        {
            _pendingRages.Clear();
        }
    }
}
