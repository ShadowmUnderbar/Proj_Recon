using App.Battle.Interface;
using App.Battle.Data;
using UnityEngine;
using R3;
using App.Common.Data;
using UnityEditor;

namespace App.Battle.Views
{
    public class PlayerTopDownAimListView : MonoBehaviour, IPlayerTopDownAimListView
    {
        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();
        public Observable<int> OnFocusLeft => _onFocusLeft;
        private readonly Subject<int> _onFocusLeft = new();
        public Observable<int> OnFocusRight => _onFocusRight;
        private readonly Subject<int> _onFocusRight = new();

        public Observable<Vector3> OnRightAimPosition => _onRightAimPosition;
        private readonly Subject<Vector3> _onRightAimPosition = new();

        public Observable<Vector3> OnLeftAimPosition => _onLeftAimPosition;
        public Vector3 MousePosition { get; set; }
        private readonly Subject<Vector3> _onLeftAimPosition = new();

        private IPlayerTopDownAimView _leftTopDown;
        private IPlayerTopDownAimView _rightTopDown;

        private IPlayerAimMuzzleView _leftAimView;
        private IPlayerAimMuzzleView _rightAimView;

        private IPlayerShotView _leftShotView;
        private IPlayerShotView _rightShotView;

        public void InitStoreView(
            IPlayerTopDownAimView leftTopDown, IPlayerTopDownAimView rightTopDown,
            IPlayerAimMuzzleView leftAim, IPlayerAimMuzzleView rightAim,
            IPlayerShotView leftShot, IPlayerShotView rightShot
        )
        {
            _leftTopDown = leftTopDown;
            _rightTopDown = rightTopDown;

            leftTopDown.OnFocus
                .Subscribe(x => _onFocusLeft.OnNext(x))
                .AddTo(this);

            rightTopDown.OnFocus
                .Subscribe(x => _onFocusRight.OnNext(x))
                .AddTo(this);

            _leftAimView = leftAim;
            _rightAimView = rightAim;

            _leftShotView = leftShot;
            _rightShotView = rightShot;
        }

        public void SetRayColor(HandType handType, Color color)
        {
            if (handType == HandType.Left)
            {
                _leftAimView.SetRayColor(color);
                return;
            }

            _rightAimView.SetRayColor(color);
        }

        public void SetEnableRay(HandType handType, bool enable)
        {
            if (handType == HandType.Left)
            {
                _leftAimView.SetEnableRay(enable);
                return;
            }

            _rightAimView.SetEnableRay(enable);
        }

        public void Aim()
        {
            var isVR = DebugConfig.IsVRMode;

            var leftAimPosition = isVR ? _leftTopDown.GetAimPosition() : MousePosition;
            var rightAimPosition = isVR ? _rightTopDown.GetAimPosition() : MousePosition;

            _onLeftAimPosition.OnNext(leftAimPosition);
            _onRightAimPosition.OnNext(rightAimPosition);

            _leftAimView?.LookAimPosition(leftAimPosition);
            _rightAimView?.LookAimPosition(rightAimPosition);
        }

        public void Shot(HandType handType, BulletData bulletData, int focusTargetId)
        {
            if (handType == HandType.Left)
            {
                _leftShotView.SpawnBullet(bulletData, focusTargetId);
                return;
            }

            _rightShotView.SpawnBullet(bulletData, focusTargetId);
        }

        public void IsFocusLeft(bool isFocus)
        {
            Debug.Log($"IsFocusLeft: {isFocus}");
            _leftTopDown.IsFocus = isFocus;
        }

        public void IsFocusRight(bool isFocus)
        {
            _rightTopDown.IsFocus = isFocus;
        }

        private void OnDestroy()
        {
            _onHit.Dispose();
            _onFocusLeft.Dispose();
            _onFocusRight.Dispose();
            _onLeftAimPosition.Dispose();
            _onRightAimPosition.Dispose();
        }
    }
}