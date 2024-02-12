using App.Battle.Views;
using App.Framework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace App.Battle.Interface.Views
{
    public class BattlePlayerView : MonoBehaviour, IBattlePlayerView
    {
        [SerializeField]
        private PlayerMoveView _playerMoveView;
        [SerializeField]
        private PlayerTopDownAimStoreView _playerTopDownAimStoreView;
        [SerializeField]
        private Transform[] _controllers;
        [SerializeField]
        private Transform[] _muzzles;

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

        public void Move(Vector2 _inputV2)
        {
            _playerMoveView.Move(_inputV2);
        }

        public void Aim() 
        {
            _playerTopDownAimStoreView.Aim();
        }

        public void SetFocus(bool isLeft)
        {
            _playerTopDownAimStoreView.Aim();
        }

        public void Shot(bool isLeft)
        {
            _playerTopDownAimStoreView.Shot(isLeft);
        }
    }
}