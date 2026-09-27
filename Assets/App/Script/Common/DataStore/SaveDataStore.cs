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

        /// <summary>セーブファイルのパス。デバッグ用エディタ拡張からも参照する</summary>
        public static string SaveDataPath => Application.dataPath + "/DLHN/SaveData.json";

        public void Initialize()
        {
            Load();
        }

        public void Save()
        {
            try
            {
                // ディレクトリパスを取得
                var directoryPath = Path.GetDirectoryName(SaveDataPath);

                // ディレクトリが存在しない場合は作成
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // JSONシリアライズ
                var json = JsonUtility.ToJson(SaveData);

                // usingでStreamWriterを管理
                using (var writer = new StreamWriter(SaveDataPath, false))
                {
                    writer.WriteLine(json);
                }

                _onSave.OnNext(Unit.Default);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveDataの保存に失敗しました: {e.Message}");
                throw;
            }
        }

        public SaveData Load()
        {
            try
            {
                // ファイルが存在しない場合は新規データを返す
                if (!File.Exists(SaveDataPath))
                {
                    SaveData = new SaveData();
                    ApplyDebugOverride();
                    _onLoad.OnNext(Unit.Default);
                    return SaveData;
                }

                // usingでStreamReaderを管理
                string json;
                using (var reader = new StreamReader(SaveDataPath))
                {
                    json = reader.ReadToEnd();
                }

                // JSONデシリアライズ
                SaveData = JsonUtility.FromJson<SaveData>(json);
                ApplyDebugOverride();
                _onLoad.OnNext(Unit.Default);
                return SaveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveDataの読み込みに失敗しました: {e.Message}");

                // 失敗時は新規データを返す
                SaveData = new SaveData();
                ApplyDebugOverride();
                _onLoad.OnNext(Unit.Default);
                return SaveData;
            }
        }

        /// <summary>
        /// デバッグ用: 「App/デバッグ: セーブデータ」で設定した内容を読み込み直後のセーブデータへ上書きする。
        /// ファイルには書かず、メモリ上の値だけ差し替える（次に Save されれば上書き後の値が残る）。
        /// 製品ビルドでは <see cref="DebugConfig.SaveDataOverrideJson"/> が常に null なので何も起きない。
        /// </summary>
        private void ApplyDebugOverride()
        {
            var json = DebugConfig.SaveDataOverrideJson;
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, SaveData);
                Debug.Log("[SaveDataStore] デバッグ設定でセーブデータを上書きしました（App/デバッグ: セーブデータ）");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataStore] デバッグ用セーブデータ上書きに失敗しました: {e.Message}");
            }
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