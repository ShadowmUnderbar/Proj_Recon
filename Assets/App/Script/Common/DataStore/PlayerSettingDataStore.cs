using System;
using App.Common.Data;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Common.DataStore
{
    public class PlayerSettingDataStore : IPlayerSettingDataStore, IInitializable, IDisposable
    {
        private readonly ISaveDataStore _saveDataStore;
        public ReactiveProperty<HandType> DominantHand { get; } = new();

        public HandType NonDominantHand => DominantHand.Value == HandType.Right ? HandType.Left : HandType.Right;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerSettingDataStore(
            ISaveDataStore saveDataStore
        )
        {
            _saveDataStore = saveDataStore;
        }

        public void Initialize()
        {
            _saveDataStore.OnLoad
                .Subscribe(_ => OnSaveUpdate())
                .AddTo(_disposable);
            _saveDataStore.OnSave
                .Subscribe(_ => OnSaveUpdate())
                .AddTo(_disposable);
        }

        private void OnSaveUpdate()
        {
            DominantHand.Value = _saveDataStore.SaveData.DominantHand;

            if (!DebugConfig.IsVRMode)
            {
                DominantHand.Value = HandType.Left;
            }
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}