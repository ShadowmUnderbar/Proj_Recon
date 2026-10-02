using App.Battle.Data;
using App.Battle.Interface.DataStore;
using VContainer;

namespace App.Battle.DataStore
{
    public class BossWaveDataStore : IBossWaveDataStore, IRunResettable
    {
        private readonly BossWaveConfig _bossWaveConfig;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IBossGroupDataStore _bossGroupDataStore;
        private readonly DebugArenaSettings _debugArenaSettings;

        // ボスを出現させたウェーブ番号（0は未出現）。ウェーブが変われば自然に「未出現」扱いになる
        private int _spawnedWave;

        [Inject]
        public BossWaveDataStore(
            BossWaveConfig bossWaveConfig,
            IWaveManagerDataStore waveManagerDataStore,
            IBossGroupDataStore bossGroupDataStore,
            DebugArenaSettings debugArenaSettings
        )
        {
            _bossWaveConfig = bossWaveConfig;
            _waveManagerDataStore = waveManagerDataStore;
            _bossGroupDataStore = bossGroupDataStore;
            _debugArenaSettings = debugArenaSettings;
        }

        private int CurrentWave => _waveManagerDataStore.CurrentWave.CurrentValue;

        // デバッグ対戦はボスウェーブの番号を指定しても、ボスウェーブとしては扱わない（選んだ相手だけを出す）
        public bool IsBossWave => !_debugArenaSettings.IsEnabled && _bossWaveConfig.IsBossWave(CurrentWave);

        public bool IsBossSpawned => IsBossWave && _spawnedWave == CurrentWave;

        public bool IsBossCleared => IsBossSpawned && !_bossGroupDataStore.HasAliveGroup;

        public bool TrySpawnBoss()
        {
            if (!IsBossWave || IsBossSpawned)
            {
                return false;
            }

            // 出現に失敗しても出現済みとして扱う（倒す相手がいないままウェーブが進まなくなるのを防ぐ。原因は SpawnGroup がエラーログに出す）
            _spawnedWave = CurrentWave;

            var memberIds = _bossGroupDataStore.SpawnGroup(_bossWaveConfig.BossGroup, _bossWaveConfig.BossOrigin);
            return memberIds.Count > 0;
        }

        public void ResetRun()
        {
            _spawnedWave = 0;
        }
    }
}
