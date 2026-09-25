using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// Localization の "Upgrade" テーブルを読み込み、アップグレードの文言を引く。
    /// テーブルは非同期読込のため、読込前は IsReady=false でキー文字列を返す
    /// </summary>
    public class UpgradeLocalizationDataStore : IUpgradeLocalizationDataStore, IInitializable, IDisposable
    {
        private const string TableName = "Upgrade";

        private readonly Subject<Unit> _onTableChanged = new();
        public Observable<Unit> OnTableChanged => _onTableChanged;

        // 未登録キーの警告を1キー1回に抑える（表示側から毎回呼ばれてもログが溢れないように）
        private readonly HashSet<string> _warnedMissingKeys = new();

        private StringTable _table;
        public bool IsReady => _table != null;

        // ロケール連続切替時に、古い読込の完了で新しいテーブルを上書きしないための世代番号
        private int _loadGeneration;
        private bool _isDisposed;

        // 強化/弱化効果の文字色（効果値と <p>/<n> タグに使う）
        private readonly UpgradeDescriptionStyle _descriptionStyle;

        [Inject]
        public UpgradeLocalizationDataStore(UpgradeDescriptionStyle descriptionStyle)
        {
            _descriptionStyle = descriptionStyle;
        }

        public void Initialize()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            LoadTable();
        }

        public UpgradeLocalizedText GetText(UpgradeMasterData upgrade)
        {
            if (upgrade == null)
            {
                throw new ArgumentNullException(nameof(upgrade));
            }

            var nameKey = upgrade.NameKey;
            var descriptionTemplate = GetString(UpgradeLocalizationKey.Description(nameKey));
            var description = UpgradeDescriptionFormatter.Format(descriptionTemplate, upgrade, _descriptionStyle);

            // 簡略説明・詳細説明は文言中の <p>/<n> を強化/弱化色に置き換える（タイトル・レベル表記は現状対象外）
            return new UpgradeLocalizedText(
                GetString(UpgradeLocalizationKey.Title(nameKey)),
                EffectTextStyler.ApplyEffectTags(GetString(UpgradeLocalizationKey.SimpleDescription(nameKey)), _descriptionStyle),
                EffectTextStyler.ApplyEffectTags(description, _descriptionStyle),
                GetString(UpgradeLocalizationKey.Level(upgrade.Level))
            );
        }

        private string GetString(string key)
        {
            if (_table == null)
            {
                return key;
            }

            var entry = _table.GetEntry(key);
            if (entry == null)
            {
                if (_warnedMissingKeys.Add(key))
                {
                    Debug.LogWarning(
                        $"[UpgradeLocalization] テーブル '{TableName}' ({_table.LocaleIdentifier.Code}) にキー '{key}' がありません");
                }

                return key;
            }

            // Smart String 化されていない前提で生の値を使う（{valueN} は Formatter で置換する）
            return entry.Value;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            LoadTable();
        }

        private void LoadTable()
        {
            var generation = ++_loadGeneration;
            var handle = LocalizationSettings.StringDatabase.GetTableAsync(TableName);

            if (handle.IsDone)
            {
                OnTableLoaded(handle, generation);
                return;
            }

            handle.Completed += h => OnTableLoaded(h, generation);
        }

        private void OnTableLoaded(AsyncOperationHandle<StringTable> handle, int generation)
        {
            if (_isDisposed || generation != _loadGeneration)
            {
                return;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"[UpgradeLocalization] テーブル '{TableName}' の読込に失敗しました: {handle.OperationException}");

                // 前ロケールのテーブルを使い続けると IsReady=true のまま別言語の文言を返すため破棄し、表示側にキー表示へ戻させる
                _table = null;
                _onTableChanged.OnNext(Unit.Default);
                return;
            }

            _table = handle.Result;
            _warnedMissingKeys.Clear();
            _onTableChanged.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _isDisposed = true;
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            _onTableChanged.Dispose();
        }
    }
}
