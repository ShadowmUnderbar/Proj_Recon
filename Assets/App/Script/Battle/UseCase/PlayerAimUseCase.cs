using App.Battle.Interface;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using R3;
using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly IPlayerFocusDataStore _playerFocusDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly IShotConflictDataStore _shotConflictDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerAimUseCase(
            IPlayerAimDataStore playerAimDataStore,
            IPlayerFocusDataStore playerFocusDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore,
            IShotConflictDataStore shotConflictDataStore
        )
        {
            _playerAimDataStore = playerAimDataStore;
            _playerFocusDataStore = playerFocusDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
            _shotConflictDataStore = shotConflictDataStore;
        }

        public void Initialize()
        {
            _playerControlPresenter.OnFocusLeft
                .Subscribe(x => UpdateOnFocus(x, true))
                .AddTo(_disposables);
            _playerControlPresenter.OnFocusRight
                .Subscribe(x => UpdateOnFocus(x, false))
                .AddTo(_disposables);

            _playerControlPresenter.OnLeftAimPosition
                .Subscribe(x => _playerAimDataStore.SetAimPosition(HandType.Left, x))
                .AddTo(_disposables);
            _playerControlPresenter.OnRightAimPosition
                .Subscribe(x => _playerAimDataStore.SetAimPosition(HandType.Right, x))
                .AddTo(_disposables);

            _playerControlPresenter.LeftHandPose
                .Subscribe(x => _playerAimDataStore.LeftHandPose.Value = x)
                .AddTo(_disposables);
            _playerControlPresenter.RightHandPose
                .Subscribe(x => _playerAimDataStore.RightHandPose.Value = x)
                .AddTo(_disposables);

            // 封印中はエイムのスナップ自体を止める（フォーカス対象を掴ませない）
            _gameInputDataStore.IsFocusLeft
                .Subscribe(x => _playerControlPresenter.IsFocusLeft(x && !_shotConflictDataStore.IsFocusLocked))
                .AddTo(_disposables);

            _gameInputDataStore.IsFocusRight
                .Subscribe(x => _playerControlPresenter.IsFocusRight(x && !_shotConflictDataStore.IsFocusLocked))
                .AddTo(_disposables);
        }

        private void UpdateOnFocus(int id, bool isLeft)
        {
            // コンフリクト系でフォーカスが封印されている間は対象を掴まない。
            // ここで弾く必要がある（Tickでの解除は同フレーム内にこの通知で上書きされてしまう）
            if (_shotConflictDataStore.IsFocusLocked)
            {
                id = -1;
            }

            if (isLeft)
            {
                _playerFocusDataStore.FocusLeftTargetId.Value = id;
                return;
            }

            _playerFocusDataStore.FocusRightTargetId.Value = id;
        }

        public void Tick()
        {
            if (!DebugConfig.IsVRMode)
            {
                _playerControlPresenter.MouseAim(_gameInputDataStore.MouseInputPosition);
            }

            _playerControlPresenter.Aim();

            // 両手エイムの中心方向へモデルを振り向かせる
            _playerControlPresenter.SetModelFacing(_playerAimDataStore.CenterAimDirection);

            // 両手のエイム対象ワールド座標を渡し、両腕のIK追従に使わせる
            _playerControlPresenter.SetAimTargets(
                _playerAimDataStore.LeftAimPosition,
                _playerAimDataStore.RightAimPosition
            );
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
