using App.Battle.Interface;
using App.Framework.Utilities;
using System.Collections.Generic;
using App.Battle.Data;
using UnityEngine;
using VContainer;
using R3;

namespace App.Battle.Views
{
    public class BattlePlayerView : MonoBehaviour, IBattlePlayerView
    {
        [SerializeField] private PlayerMoveView _playerMoveView;
        [SerializeField] private PlayerTopDownAimStoreView _playerTopDownAimStoreView;
        [SerializeField] private Transform[] _controllers;
        [SerializeField] private Transform[] _muzzles;

        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private ISimpleObjectFactory<IPlayerTopDownAimView> _topDownFactory;
        private ISimpleObjectFactory<IPlayerAimMuzzleView> _aimFactory;
        private ISimpleObjectFactory<IPlayerShotView> _shotFactory;

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IPlayerTopDownAimView> topDownFactory,
            ISimpleObjectFactory<IPlayerAimMuzzleView> aimFactory,
            ISimpleObjectFactory<IPlayerShotView> shotFactory
        )
        {
            _topDownFactory = topDownFactory;
            _aimFactory = aimFactory;
            _shotFactory = shotFactory;

            var topdownViews = new List<IPlayerTopDownAimView>();
            var aimViews = new List<IPlayerAimMuzzleView>();
            var shotViews = new List<IPlayerShotView>();

            foreach (var view in _controllers)
            {
                topdownViews.Add(_topDownFactory.Instantiate(view));
            }

            foreach (var view in _muzzles)
            {
                var aimView = _aimFactory.Instantiate(view);
                aimViews.Add(aimView);
                shotViews.Add(_shotFactory.Instantiate(aimView.Transform));
            }

            _playerTopDownAimStoreView.Initialize(topdownViews, aimViews, shotViews);
        }

        private void Awake()
        {
            _onHit.AddTo(this);
            _playerTopDownAimStoreView.OnHit.Subscribe(OnHitBullet).AddTo(this);
        }

        public void Move(Vector2 _inputV2, float speed)
        {
            _playerMoveView.Move(_inputV2, speed);
        }

        public void Aim()
        {
            _playerTopDownAimStoreView.Aim();
        }

        public void MouseAim(Vector2 mousePos)
        {
            if (Camera.main == null)
            {
                return;
            }

            var ray = Camera.main.ScreenPointToRay(mousePos);

            var targetPos = new Vector3(mousePos.x, 0, mousePos.y);

            if (Physics.Raycast(ray, out var hit, 100f))
            {
                targetPos = hit.point;
            }

            foreach (var controller in _controllers)
            {
                controller.LookAt(targetPos);
            }
        }

        public void SetFocus(bool isLeft)
        {
            _playerTopDownAimStoreView.Aim();
        }

        public void Shot(bool isLeft)
        {
            _playerTopDownAimStoreView.Shot(isLeft);
        }

        private void OnHitBullet(HitData hit)
        {
            _onHit.OnNext(hit);
        }
    }
}