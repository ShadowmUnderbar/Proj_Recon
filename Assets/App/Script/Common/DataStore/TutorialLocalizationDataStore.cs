using System;
using System.Collections.Generic;
using App.Common.Data;
using App.Common.Interface;
using R3;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace App.Common.DataStore
{
    /// <summary>
    /// Localization の "TutorialText" テーブルを読み込み、チュートリアルの本文を引く。
    /// テーブルは非同期読込のため、読込前は IsReady=false でキー文字列を返す。
    /// 読込・ロケール追従の流れは UpgradeLocalizationDataStore と同じ
    /// </summary>
    public class TutorialLocalizationDataStore : ITutorialLocalizationDataStore, IInitializable, IDisposable
    {
        private const string TableName = "TutorialText";

        private readonly Subject<Unit> _onTableChanged = new();
        public Observable<Unit> OnTableChanged => _onTableChanged;

        // 未登録キーの警告を1キー1回に抑える（表示側から毎回呼ばれてもログが溢れないように）
        private readonly HashSet<string> _warnedMissingKeys = new();

        private StringTable _table;
        public bool IsReady => _table != null;

        // ロケール連続切替時に、古い読込の完了で新しいテーブルを上書きしないための世代番号
        private int _loadGeneration;
        private bool _isDisposed;

        // 本文中の色タグ（<p>/<n>/<w> など）の文字色
        private readonly EffectTextStyle _textStyle;

        [Inject]
        public TutorialLocalizationDataStore(EffectTextStyle textStyle)
        {
            _textStyle = textStyle;
        }

        public void Initialize()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            LoadTable();
        }

        public string GetText(TutorialType type)
        {
            return EffectTextStyler.ApplyEffectTags(GetString(TutorialLocalizationKey.Text(type)), _textStyle);
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
                        $"[TutorialLocalization] テーブル '{TableName}' ({_table.LocaleIdentifier.Code}) にキー '{key}' がありません");
                }

                return key;
            }

            // Smart String 化されていない前提で生の値を使う
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
                Debug.LogError($"[TutorialLocalization] テーブル '{TableName}' の読込に失敗しました: {handle.OperationException}");

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
