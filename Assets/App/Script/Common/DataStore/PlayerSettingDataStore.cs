using System;
using App.Common.Data;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Common.DataStore
{
    public class PlayerSettingDataStore : IPlayerSettingDataStore, IInitializable, IDisposable
    {
        private readonly ISaveDataStore _saveDataStore;
        public ReactiveProperty<HandType> DominantHand { get; } = new();

        public HandType NonDominantHand => DominantHand.Value == HandType.Right ? HandType.Left : HandType.Right;

        public ReactiveProperty<LocomotionType> Locomotion { get; } = new();
        public ReactiveProperty<float> MoveSpeed { get; } = new(PlayerSettingRange.DefaultMoveSpeed);
        public ReactiveProperty<int> SnapTurnAngle { get; } = new(PlayerSettingRange.DefaultSnapTurnAngle);

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

            Locomotion.Value = _saveDataStore.SaveData.Locomotion;
            MoveSpeed.Value = ClampMoveSpeed(_saveDataStore.SaveData.MoveSpeed);
            SnapTurnAngle.Value = ClampSnapTurnAngle(_saveDataStore.SaveData.SnapTurnAngle);
        }

        public void SetDominantHand(HandType hand)
        {
            // 非VRでは利き手を左に固定して扱っている（OnSaveUpdate参照）。
            // ここで保存だけ通すと、画面の選択は左のままセーブデータだけ右になってしまう
            if (!DebugConfig.IsVRMode)
            {
                return;
            }

            if (_saveDataStore.SaveData.DominantHand == hand)
            {
                return;
            }

            _saveDataStore.SaveData.DominantHand = hand;
            _saveDataStore.Save();
        }

        public void SetLocomotion(LocomotionType locomotion)
        {
            if (_saveDataStore.SaveData.Locomotion == locomotion)
            {
                return;
            }

            _saveDataStore.SaveData.Locomotion = locomotion;
            _saveDataStore.Save();
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            var clamped = ClampMoveSpeed(moveSpeed);

            // スライダー操作は毎フレーム値が飛んでくるため、変化がなければ保存しない
            if (Mathf.Approximately(_saveDataStore.SaveData.MoveSpeed, clamped))
            {
                return;
            }

            _saveDataStore.SaveData.MoveSpeed = clamped;
            _saveDataStore.Save();
        }

        public void SetSnapTurnAngle(int angle)
        {
            var clamped = ClampSnapTurnAngle(angle);

            if (_saveDataStore.SaveData.SnapTurnAngle == clamped)
            {
                return;
            }

            _saveDataStore.SaveData.SnapTurnAngle = clamped;
            _saveDataStore.Save();
        }

        private static float ClampMoveSpeed(float moveSpeed) =>
            Mathf.Clamp(moveSpeed, PlayerSettingRange.MinMoveSpeed, PlayerSettingRange.MaxMoveSpeed);

        /// <summary>スナップターン角度を刻み幅へ丸めたうえで範囲内に収める</summary>
        private static int ClampSnapTurnAngle(int angle)
        {
            var stepped = Mathf.RoundToInt(angle / (float)PlayerSettingRange.SnapTurnAngleStep) *
                          PlayerSettingRange.SnapTurnAngleStep;

            return Mathf.Clamp(stepped, PlayerSettingRange.MinSnapTurnAngle, PlayerSettingRange.MaxSnapTurnAngle);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
