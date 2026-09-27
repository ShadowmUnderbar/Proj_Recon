using System.Collections.Generic;
using App.Common.Data;
using App.Common.Interface;
using VContainer;

namespace App.Common.DataStore
{
    /// <summary>
    /// チュートリアルの閲覧進行を管理する。
    /// 実体は <see cref="ISaveDataStore"/> の <see cref="SaveData.TutorialViews"/>。
    /// 同じチュートリアルを <see cref="RequiredViewCount"/> 回閲覧すると閲覧済みになり、
    /// 設定で再表示が有効でない限り表示しない。
    /// </summary>
    public class TutorialProgressDataStore : ITutorialProgressDataStore
    {
        /// <summary>この回数閲覧したら閲覧済み扱いにする</summary>
        public const int RequiredViewCount = 3;

        private readonly ISaveDataStore _saveDataStore;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        [Inject]
        public TutorialProgressDataStore(
            ISaveDataStore saveDataStore,
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _saveDataStore = saveDataStore;
            _playerSettingDataStore = playerSettingDataStore;
        }

        public int GetViewCount(TutorialType type)
        {
            var record = FindRecord(type);
            return record?.ViewCount ?? 0;
        }

        public bool IsCompleted(TutorialType type)
        {
            return GetViewCount(type) >= RequiredViewCount;
        }

        public bool ShouldShow(TutorialType type)
        {
            if (_playerSettingDataStore.IsTutorialReplayEnabled.CurrentValue)
            {
                return true;
            }

            return !IsCompleted(type);
        }

        public void MarkViewed(TutorialType type)
        {
            var record = FindRecord(type);
            if (record == null)
            {
                record = new TutorialViewRecord { Type = type };
                EnsureRecords().Add(record);
            }

            // 完了後も数え続けると再表示ONのときに際限なく増えるため上限で止める
            if (record.ViewCount >= RequiredViewCount)
            {
                return;
            }

            record.ViewCount++;
            _saveDataStore.Save();
        }

        public void ResetProgress(TutorialType type)
        {
            var removed = EnsureRecords().RemoveAll(r => r.Type == type);
            if (removed == 0)
            {
                return;
            }

            _saveDataStore.Save();
        }

        private TutorialViewRecord FindRecord(TutorialType type)
        {
            foreach (var record in EnsureRecords())
            {
                if (record.Type == type)
                {
                    return record;
                }
            }

            return null;
        }

        /// <summary>旧セーブデータではリストが null のことがあるため、必ず実体を用意する</summary>
        private List<TutorialViewRecord> EnsureRecords()
        {
            var list = _saveDataStore.SaveData.TutorialViews;
            if (list == null)
            {
                list = new List<TutorialViewRecord>();
                _saveDataStore.SaveData.TutorialViews = list;
            }

            return list;
        }
    }
}
