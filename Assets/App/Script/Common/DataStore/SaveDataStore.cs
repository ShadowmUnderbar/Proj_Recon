using System;
using System.IO;
using App.Common.Data;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Common.DataStore
{
    public class SaveDataStore : ISaveDataStore, IInitializable, IDisposable
    {
        public Observable<Unit> OnLoad => _onLoad;
        private readonly Subject<Unit> _onLoad = new();

        public Observable<Unit> OnSave => _onSave;
        private readonly Subject<Unit> _onSave = new();
        public SaveData SaveData { get; private set; } = new();

        private string SaveDataPath => Application.dataPath + "/DLHN/SaveData.json";

        public void Initialize()
        {
            Load();
        }

        public void Save()
        {
            //ディレクトリがあるか
            if (!Directory.Exists(SaveDataPath))
            {
                //ディレクトリ作成
                Directory.CreateDirectory(SaveDataPath);
            }

            var json = JsonUtility.ToJson(SaveData);
            var wr = new StreamWriter(SaveDataPath, false);
            wr.WriteLine(json);
            wr.Close();
            _onSave.OnNext(Unit.Default);
        }

        public SaveData Load()
        {
            if (!Directory.Exists(SaveDataPath))
            {
                _onLoad.OnNext(Unit.Default);
                return new SaveData();
            }

            var rd = new StreamReader(SaveDataPath);
            var json = rd.ReadToEnd();
            rd.Close();

            SaveData = JsonUtility.FromJson<SaveData>(json);
            _onLoad.OnNext(Unit.Default);
            return SaveData;
        }

        public void ResetSaveData()
        {
            SaveData = new SaveData();
            Save();
        }

        public void Dispose()
        {
            _onLoad?.Dispose();
            _onSave?.Dispose();
        }
    }
}