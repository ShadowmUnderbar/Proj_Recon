using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerShotTypeDataStore : IPlayerShotTypeDataStore, IInitializable, ITickable
    {
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IExtraConflictDataStore _extraConflictDataStore;

        public ReactiveProperty<ShotType> ShotType { get; } = new();

        private const float MergePositionDistance = 0.15f;
        private const float WaltzAngleDifference = 130f;

        [Inject]
        public PlayerShotTypeDataStore(
            IPlayerAimDataStore playerAimDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IGameInputDataStore gameInputDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IExtraConflictDataStore extraConflictDataStore
        )
        {
            _playerAimDataStore = playerAimDataStore;
            _playerStateDataStore = playerStateDataStore;
            _gameInputDataStore = gameInputDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _extraConflictDataStore = extraConflictDataStore;
        }

        public void Initialize()
        {
            ShotType.Value = Common.Data.ShotType.Normal;
        }

        public void Tick()
        {
            if (!DebugConfig.IsVRMode)
            {
                UpdateShotType_PC();
            }
            else
            {
                UpdateShotType();
            }
        }

        private void UpdateShotType()
        {
            if (IsMerge() &&
                _coreSkillUnlockDataStore.IsUnLockMerge &&
                !_extraConflictDataStore.IsShotTypeLocked(Common.Data.ShotType.Merge))
            {
                ShotType.Value = Common.Data.ShotType.Merge;
                return;
            }

            if (IsWaltz() &&
                _coreSkillUnlockDataStore.IsUnLockWaltz &&
                !_extraConflictDataStore.IsShotTypeLocked(Common.Data.ShotType.Waltz))
            {
                ShotType.Value = Common.Data.ShotType.Waltz;
                return;
            }

            ShotType.Value = Common.Data.ShotType.Normal;
        }

        private void UpdateShotType_PC()
        {
            // PCモードはキーを押した時だけ書き換える方式のため、
            // 封印されたフォームのまま固定されると発射できなくなる。先にノーマルへ戻す
            if (_extraConflictDataStore.IsShotTypeLocked(ShotType.Value))
            {
                ShotType.Value = Common.Data.ShotType.Normal;
            }

            if (_gameInputDataStore.DebugMerge.Value &&
                _coreSkillUnlockDataStore.IsUnLockMerge &&
                !_extraConflictDataStore.IsShotTypeLocked(Common.Data.ShotType.Merge))
            {
                ShotType.Value = Common.Data.ShotType.Merge;
                return;
            }

            if (_gameInputDataStore.DebugWaltz.Value &&
                _coreSkillUnlockDataStore.IsUnLockWaltz &&
                !_extraConflictDataStore.IsShotTypeLocked(Common.Data.ShotType.Waltz))
            {
                ShotType.Value = Common.Data.ShotType.Waltz;
                return;
            }

            if (_gameInputDataStore.DebugNormal.Value)
            {
                ShotType.Value = Common.Data.ShotType.Normal;
            }
        }

        private bool IsMerge()
        {
            var distance = (_playerAimDataStore.LeftHandPose.Value.position -
                            _playerAimDataStore.RightHandPose.Value.position).sqrMagnitude;
            return Mathf.Abs(distance) < MergePositionDistance * MergePositionDistance;
        }

        private bool IsWaltz()
        {
            var leftAimDirection = _playerAimDataStore.LeftAimDirection;
            var rightAimDirection = _playerAimDataStore.RightAimDirection;

            leftAimDirection = Vector3.ProjectOnPlane(leftAimDirection, Vector3.up);
            rightAimDirection = Vector3.ProjectOnPlane(rightAimDirection, Vector3.up);
            var angleDifference = Vector3.SignedAngle(leftAimDirection, rightAimDirection, Vector3.up);
            return Mathf.Abs(angleDifference) >= WaltzAngleDifference;
        }
    }
}
