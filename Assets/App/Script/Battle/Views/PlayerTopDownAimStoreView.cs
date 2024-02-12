using App.Battle.Interface.Views;
using App.Framework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour , IPlayerTopDownAimStoreView
    {
        [SerializeField]
        private Transform[] _controllers;
        [SerializeField]
        private Transform[] _muzzles;

        private ISimpleObjectFactory<IPlayerTopDownAimView> _topDownFactory;
        private readonly List<IPlayerTopDownAimView> _topdownViews = new();

        private ISimpleObjectFactory<IPlayerAimMuzzleView> _aimFactory;
        private readonly List<IPlayerAimMuzzleView> _aimViews = new();

        private ISimpleObjectFactory<IPlayerShotView> _shotFactory;
        private readonly List<IPlayerShotView> _shotViews = new();

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

            foreach(var view in _controllers)
            {
                _topdownViews.Add(_topDownFactory.Instantiate(view));
            }

            foreach(var view in _muzzles)
            {
                var aimView = _aimFactory.Instantiate(view);
                _aimViews.Add(aimView);
                _shotViews.Add(_shotFactory.Instantiate(aimView.Transform));
            }
        }

        public void Aim()
        {
            if(_aimViews == null)
            {
                return;
            }

            for(var i = 0;i< _aimViews.Count;i++)
            {
                _aimViews[i].LookAimPosition(GetAimPosition(i == 0));
            }
        }

        public Vector3 GetAimPosition(bool isLeft)
        {
            if (_topdownViews == null)
            {
                return default;
            }

            return _topdownViews[isLeft ? 0 : 1].GetAimPosition();
        }

        public bool IsFocus(bool isLeft)
        {
            if (_topdownViews == null)
            {
                return default;
            }
            return _topdownViews[isLeft ? 0 : 1].IsFocus();
        }
        public void Shot(bool isLeft)
        {
            if (_shotViews == null)
            {
                return;
            }
            _shotViews[isLeft ? 0 : 1].SpawnBullet();
        }
    }
}