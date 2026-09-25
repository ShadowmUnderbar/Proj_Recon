using System;
using App.Common.Data;
using App.Common.Interface;
using App.MainMenu.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu.UseCase
{
    /// <summary>
    /// メインメニューの部屋を歩き回る操作。入力を読んで移動方式ごとの振る舞いに振り分ける。
    ///
    /// スムーズ移動: 左スティックで歩き、右スティックの左右でスナップターン。
    /// テレポート: 左スティックを倒している間だけ照準を出し、離した瞬間に着地点へ跳ぶ。
    /// どちらの方式でもスナップターンは共通で使える。
    /// </summary>
    public class MenuLocomotionUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IMenuLocomotionPresenter _menuLocomotionPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        /// <summary>スティックの遊び。これ以下の入力は無視する</summary>
        private const float StickDeadZone = 0.15f;

        /// <summary>スナップターンが発動するスティックの倒し量</summary>
        private const float SnapTurnTriggerThreshold = 0.7f;

        /// <summary>スナップターンの連続発動を解除するスティックの戻し量</summary>
        private const float SnapTurnReleaseThreshold = 0.3f;

        /// <summary>テレポートの照準を出すスティックの倒し量</summary>
        private const float TeleportAimThreshold = 0.5f;

        /// <summary>スナップターンを1回ぶん消費済みか。倒しっぱなしで回り続けないようにする</summary>
        private bool _isSnapTurnConsumed;

        /// <summary>テレポートの照準中か</summary>
        private bool _isTeleportAiming;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public MenuLocomotionUseCase(
            IMenuLocomotionPresenter menuLocomotionPresenter,
            IGameInputDataStore gameInputDataStore,
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _menuLocomotionPresenter = menuLocomotionPresenter;
            _gameInputDataStore = gameInputDataStore;
            _playerSettingDataStore = playerSettingDataStore;
        }

        public void Initialize()
        {
            // 移動方式を切り替えた瞬間に照準が出しっぱなしにならないよう畳む
            _playerSettingDataStore.Locomotion
                .Subscribe(_ => CancelTeleportAim())
                .AddTo(_disposable);
        }

        public void Tick()
        {
            UpdateSnapTurn(_gameInputDataStore.V2RightAxis);

            var moveInput = _gameInputDataStore.V2LeftAxis;

            if (_playerSettingDataStore.Locomotion.Value == LocomotionType.Teleport)
            {
                UpdateTeleport(moveInput);
                return;
            }

            UpdateSmoothMove(moveInput);
        }

        private void UpdateSmoothMove(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < StickDeadZone * StickDeadZone)
            {
                // 入力が無くても接地を保つために移動処理自体は呼ぶ
                _menuLocomotionPresenter.Move(Vector2.zero, 0f);
                return;
            }

            _menuLocomotionPresenter.Move(moveInput, _playerSettingDataStore.MoveSpeed.Value);
        }

        private void UpdateTeleport(Vector2 moveInput)
        {
            // テレポート中も落下は必要なので、移動量ゼロで呼んで接地を保つ
            _menuLocomotionPresenter.Move(Vector2.zero, 0f);

            var isAiming = moveInput.sqrMagnitude >= TeleportAimThreshold * TeleportAimThreshold;

            if (isAiming == _isTeleportAiming)
            {
                return;
            }

            _isTeleportAiming = isAiming;

            if (isAiming)
            {
                _menuLocomotionPresenter.SetTeleportAiming(true);
                return;
            }

            // スティックを離した瞬間が決定。着地できていなければ何も起きない
            _menuLocomotionPresenter.TeleportToAim();
            _menuLocomotionPresenter.SetTeleportAiming(false);
        }

        private void UpdateSnapTurn(Vector2 turnInput)
        {
            var horizontal = turnInput.x;

            if (Mathf.Abs(horizontal) < SnapTurnReleaseThreshold)
            {
                _isSnapTurnConsumed = false;
                return;
            }

            if (_isSnapTurnConsumed || Mathf.Abs(horizontal) < SnapTurnTriggerThreshold)
            {
                return;
            }

            _isSnapTurnConsumed = true;
            _menuLocomotionPresenter.SnapTurn(Mathf.Sign(horizontal) * _playerSettingDataStore.SnapTurnAngle.Value);
        }

        private void CancelTeleportAim()
        {
            if (!_isTeleportAiming)
            {
                return;
            }

            _isTeleportAiming = false;
            _menuLocomotionPresenter.SetTeleportAiming(false);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
