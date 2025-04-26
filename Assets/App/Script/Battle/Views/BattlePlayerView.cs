using App.Battle.Interface;
using App.Framework.Utilities;
using System.Collections.Generic;
using System.Linq;
using App.Battle.Data;
using UnityEngine;
using VContainer;
using R3;
using App.Common.Data;
using App.Framework.Utilities.Extensions;

namespace App.Battle.Views
{
    public class BattlePlayerView : MonoBehaviour, IBattlePlayerView
    {
        [SerializeField] private PlayerMoveView _playerMoveView;
        [SerializeField] private PlayerTopDownAimListView _playerTopDownAimListView;
        [SerializeField] private Transform[] _controllers;
        [SerializeField] private Transform[] _muzzles;

        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();
        public Observable<int> OnFocusLeft => _playerTopDownAimListView.OnFocusLeft;
        public Observable<int> OnFocusRight => _playerTopDownAimListView.OnFocusRight;

        public Observable<Vector3> OnLeftAimPosition => _playerTopDownAimListView.OnLeftAimPosition;
        public Observable<Vector3> OnRightAimPosition => _playerTopDownAimListView.OnRightAimPosition;

        public ReactiveProperty<Vector3> OnUpdatePosition => _playerMoveView.OnUpdatePosition;

        private ISimpleObjectFactory<IPlayerTopDownAimView> _topDownFactory;
        private ISimpleObjectFactory<IPlayerAimMuzzleView> _aimFactory;
        private ISimpleObjectFactory<IPlayerShotView> _shotFactory;

        public ReactiveProperty<Pose> LeftHandPose { get; } = new();
        public ReactiveProperty<Pose> RightHandPose { get; } = new();

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

            var aimViews = new List<IPlayerAimMuzzleView>();
            var shotViews = new List<IPlayerShotView>();

            var topdownViews = _controllers.Select(view => _topDownFactory.Instantiate(view)).ToList();

            foreach (var view in _muzzles)
            {
                var aimView = _aimFactory.Instantiate(view);
                aimViews.Add(aimView);
                shotViews.Add(_shotFactory.Instantiate(aimView.Transform));
            }

            _playerTopDownAimListView.InitStoreView(
                topdownViews[0], topdownViews[1],
                aimViews[0], aimViews[1],
                shotViews[0], shotViews[1]
            );
        }

        private void Awake()
        {
            _onHit.AddTo(this);
            _playerTopDownAimListView.OnHit.Subscribe(OnHitBullet).AddTo(this);
        }

        private void Update()
        {
            LeftHandPose.Value = _controllers[0].ToPose();
            RightHandPose.Value = _controllers[1].ToPose();
        }

        public void Move(Vector2 inputV2, float speed)
        {
            _playerMoveView.Move(inputV2, speed);
        }

        public void Aim()
        {
            _playerTopDownAimListView.Aim();
        }

        public void SetRayColor(HandType handType, Color color)
        {
            _playerTopDownAimListView.SetRayColor(handType, color);
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

        public void Shot(HandType handType, ShotType shotType, AimFocusType focusType, int focusTargetId)
        {
            _playerTopDownAimListView.Shot(handType, shotType, focusType, focusTargetId);
        }

        private void OnHitBullet(HitData hit)
        {
            _onHit.OnNext(hit);
        }

        private void OnDestroy()
        {
            _onHit.Dispose();
        }
    }
}