using System;
using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class RunLevelUseCase : IInitializable, IDisposable
    {
        private readonly IRunLevelDataStore _runLevelDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IRunLevelPresenter _runLevelPresenter;
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly RunLevelConfig _config;

        private readonly CompositeDisposable _disposable = new();

        private int _pendingLevelUps = 0;
        private bool _isSelectingUpgrade = false;

        [Inject]
        public RunLevelUseCase(
            IRunLevelDataStore runLevelDataStore,
            IEnemyDataStore enemyDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IRunLevelPresenter runLevelPresenter,
            UpgradeDatabase upgradeDatabase,
            RunLevelConfig config
        )
        {
            _runLevelDataStore = runLevelDataStore;
            _enemyDataStore = enemyDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _playerStateDataStore = playerStateDataStore;
            _runLevelPresenter = runLevelPresenter;
            _upgradeDatabase = upgradeDatabase;
            _config = config;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyDead
                .Subscribe(OnEnemyDead)
                .AddTo(_disposable);

            _runLevelDataStore.OnLevelUp
                .Subscribe(_ => OnLevelUp())
                .AddTo(_disposable);

            _runLevelPresenter.OnUpgradeSelected
                .Subscribe(OnUpgradeSelected)
                .AddTo(_disposable);
        }

        private void OnEnemyDead(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData)) return;
            if (!_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyCode, out var masterData)) return;

            var exp = _config.GetExperienceByRank(masterData.EnemyRankType);
            _runLevelDataStore.AddExperience(exp);
        }

        private void OnLevelUp()
        {
            _pendingLevelUps++;
            if (!_isSelectingUpgrade)
            {
                ShowNextLevelUp();
            }
        }

        private void ShowNextLevelUp()
        {
            if (_pendingLevelUps <= 0) return;

            _pendingLevelUps--;
            var options = SelectUpgradeOptions();
            if (options.Count == 0)
            {
                // 選択肢がない場合は次のレベルアップをそのまま処理
                if (_pendingLevelUps > 0) ShowNextLevelUp();
                return;
            }

            _isSelectingUpgrade = true;
            _waveManagerDataStore.SetWavePause(true);
            _runLevelPresenter.ShowUpgradeSelection(options);
        }

        private void OnUpgradeSelected(UpgradeMasterData upgradeData)
        {
            _upgradeSessionDataStore.AddUpgrade(upgradeData);
            _isSelectingUpgrade = false;

            if (_pendingLevelUps > 0)
            {
                // 連続レベルアップ：次の選択肢を表示（ポーズ継続）
                ShowNextLevelUp();
            }
            else
            {
                _runLevelPresenter.HideUpgradeSelection();
                _waveManagerDataStore.SetWavePause(false);
            }
        }

        /// <summary>
        /// 各 UpgradeType の「解放済みかつ未取得の最低レベル」から choiceCount 個をランダム抽出
        /// </summary>
        private IReadOnlyList<UpgradeMasterData> SelectUpgradeOptions()
        {
            var applied = _upgradeSessionDataStore.AppliedUpgrades;
            var playerUnlock = _playerStateDataStore.UnlockCoreSkillType;
            var candidates = new List<UpgradeMasterData>();

            foreach (UpgradeType upgradeType in Enum.GetValues(typeof(UpgradeType)))
            {
                if (upgradeType == UpgradeType.None) continue;

                var next = _upgradeDatabase.UpgradeMasterData
                    .Where(u => u.UpgradeType == upgradeType && IsUnlocked(u.PlayerUnlockType, playerUnlock))
                    .OrderBy(u => u.Level)
                    .FirstOrDefault(u => !applied.Contains(u.Id));

                if (next != null) candidates.Add(next);
            }

            return candidates
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(_config.UpgradeChoiceCount)
                .ToList();
        }

        private static bool IsUnlocked(PlayerUnlockType required, UnlockCoreSkillType current)
        {
            var threshold = required switch
            {
                PlayerUnlockType.None => UnlockCoreSkillType.First,
                PlayerUnlockType.Akimbo => UnlockCoreSkillType.Akimbo,
                PlayerUnlockType.Focus => UnlockCoreSkillType.Focus,
                PlayerUnlockType.Waltz => UnlockCoreSkillType.Waltz,
                PlayerUnlockType.Merge => UnlockCoreSkillType.Marge,
                PlayerUnlockType.Blitz => UnlockCoreSkillType.Blitz,
                _ => UnlockCoreSkillType.First
            };
            return current >= threshold;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
