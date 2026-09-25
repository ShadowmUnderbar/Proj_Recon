using System;
using App.Common.Interface;
using App.MainMenu.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu.UseCase
{
    /// <summary>
    /// オプションパネルの操作を設定へ反映する。
    /// 表示は常にDataStore側の値を流し込み、パネルの操作は保存を依頼するだけにして、
    /// 「画面の見た目」と「保存された設定」がずれないようにしている。
    /// </summary>
    public class OptionUseCase : IInitializable, IDisposable
    {
        private readonly IOptionPanelPresenter _optionPanelPresenter;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        /// <summary>スライダー操作を保存するまでの待ち時間[s]。ドラッグ中に毎フレーム書き込まないようにする</summary>
        private static readonly TimeSpan SliderSaveDelay = TimeSpan.FromSeconds(0.3);

        /// <summary>保存待ちの移動速度。待ち時間の途中でシーンを抜けても取りこぼさないよう保持する</summary>
        private float? _pendingMoveSpeed;

        /// <summary>保存待ちのスナップターン角度</summary>
        private int? _pendingSnapTurnAngle;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public OptionUseCase(
            IOptionPanelPresenter optionPanelPresenter,
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _optionPanelPresenter = optionPanelPresenter;
            _playerSettingDataStore = playerSettingDataStore;
        }

        public void Initialize()
        {
            #region 設定 → パネル表示

            _playerSettingDataStore.DominantHand
                .Subscribe(hand => _optionPanelPresenter.SetDominantHand(hand))
                .AddTo(_disposable);

            _playerSettingDataStore.Locomotion
                .Subscribe(locomotion => _optionPanelPresenter.SetLocomotion(locomotion))
                .AddTo(_disposable);

            _playerSettingDataStore.MoveSpeed
                .Subscribe(moveSpeed => _optionPanelPresenter.SetMoveSpeed(moveSpeed))
                .AddTo(_disposable);

            _playerSettingDataStore.SnapTurnAngle
                .Subscribe(angle => _optionPanelPresenter.SetSnapTurnAngle(angle))
                .AddTo(_disposable);

            #endregion

            #region パネル操作 → 設定の保存

            _optionPanelPresenter.OnDominantHandChanged
                .Subscribe(hand => _playerSettingDataStore.SetDominantHand(hand))
                .AddTo(_disposable);

            _optionPanelPresenter.OnLocomotionChanged
                .Subscribe(locomotion => _playerSettingDataStore.SetLocomotion(locomotion))
                .AddTo(_disposable);

            _optionPanelPresenter.OnMoveSpeedChanged
                .Subscribe(moveSpeed => _pendingMoveSpeed = moveSpeed)
                .AddTo(_disposable);

            _optionPanelPresenter.OnMoveSpeedChanged
                .Debounce(SliderSaveDelay)
                .Subscribe(_ => SaveMoveSpeed())
                .AddTo(_disposable);

            _optionPanelPresenter.OnSnapTurnAngleChanged
                .Subscribe(angle => _pendingSnapTurnAngle = angle)
                .AddTo(_disposable);

            _optionPanelPresenter.OnSnapTurnAngleChanged
                .Debounce(SliderSaveDelay)
                .Subscribe(_ => SaveSnapTurnAngle())
                .AddTo(_disposable);

            #endregion
        }

        private void SaveMoveSpeed()
        {
            if (_pendingMoveSpeed == null)
            {
                return;
            }

            _playerSettingDataStore.SetMoveSpeed(_pendingMoveSpeed.Value);
            _pendingMoveSpeed = null;

            // クランプ後の値へ表示を揃える。保存値が変わらなかったときは
            // ReactivePropertyが発火せず、スライダーだけ範囲外を指したままになる
            _optionPanelPresenter.SetMoveSpeed(_playerSettingDataStore.MoveSpeed.Value);
        }

        private void SaveSnapTurnAngle()
        {
            if (_pendingSnapTurnAngle == null)
            {
                return;
            }

            _playerSettingDataStore.SetSnapTurnAngle(_pendingSnapTurnAngle.Value);
            _pendingSnapTurnAngle = null;

            _optionPanelPresenter.SetSnapTurnAngle(_playerSettingDataStore.SnapTurnAngle.Value);
        }

        public void Dispose()
        {
            // 待ち時間の途中でシーンを抜けても、直前の操作を保存してから畳む
            SaveMoveSpeed();
            SaveSnapTurnAngle();

            _disposable.Dispose();
        }
    }
}
