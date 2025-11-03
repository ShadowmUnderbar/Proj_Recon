using System.IO;
using App.Common.Data;
using App.Common.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Common.DataStore
{
    public class SaveDataStore : ISaveDataStore, IInitializable
    {
        public SaveData SaveData { get; private set; } = new();

        private string SaveDataPath => Application.dataPath + "/DLHN/SaveData/";

        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        [Inject]
        public SaveDataStore(
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
        }

        public void Initialize()
        {
            Load();
            _playerSettingDataStore.DominantHand.Value = SaveData.DominantHand;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(SaveData);
            var wr = new StreamWriter(SaveDataPath, false);
            wr.WriteLine(json);
            wr.Close();
        }

        public SaveData Load()
        {
            var rd = new StreamReader(SaveDataPath);
            var json = rd.ReadToEnd();
            rd.Close();

            SaveData = JsonUtility.FromJson<SaveData>(json);
            return SaveData;
        }
    }
}