using App.Battle.Interface;
using App.Battle.Data;
using UnityEngine;
using R3;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour, IPlayerTopDownAimStoreView
    {
        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();
        public Observable<int> OnFocusLeft => _onFocusLeft;
        private readonly Subject<int> _onFocusLeft = new();
        public Observable<int> OnFocusRight => _onFocusRight;
        private readonly Subject<int> _onFocusRight = new();

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
                .Subscribe(x => _onFocusLeft.OnNext(x))
                .AddTo(this);

            _leftAimView = leftAim;
            _rightAimView = rightAim;

            _leftShotView = leftShot;
            _rightShotView = rightShot;

            _leftShotView.OnHit
                .Subscribe(x => _onHit.OnNext(x))
                .AddTo(this);

            _rightShotView.OnHit
                .Subscribe(x => _onHit.OnNext(x))
                .AddTo(this);
        }

        public void Aim()
        {
            _leftAimView?.LookAimPosition(GetAimPosition(true));
            _rightAimView?.LookAimPosition(GetAimPosition(false));
        }

        public Vector3 GetAimPosition(bool isLeft)
        {
            if (isLeft)
            {
                return _leftTopDown.GetAimPosition();
            }
            
            return _rightTopDown.GetAimPosition();
        }

        public void Shot(bool isLeft)
        {
            if (isLeft)
            {
                _leftShotView.SpawnBullet();
                return;
            }

            _rightShotView.SpawnBullet();
        }
    }
}