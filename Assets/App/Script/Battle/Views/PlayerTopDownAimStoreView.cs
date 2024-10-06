using System;
using App.Battle.Interface;
using System.Collections.Generic;
using App.Battle.Data;
using UniRx;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour, IPlayerTopDownAimStoreView
    {
        public IObservable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private List<IPlayerTopDownAimView> _topdownViews = new();

        private List<IPlayerAimMuzzleView> _aimViews = new();

        private List<IPlayerShotView> _shotViews = new();

        public void Initialize(
            List<IPlayerTopDownAimView> topDownFactory,
            List<IPlayerAimMuzzleView> aimFactory,
            List<IPlayerShotView> shotFactory
        )
        {
            _topdownViews = topDownFactory;
            _aimViews = aimFactory;
            _shotViews = shotFactory;

            _onHit.AddTo(this);
            foreach (var view in _shotViews)
            {
                view.OnHit.Subscribe(_onHit).AddTo(this);
            }
        }

        public void Aim()
        {
            if (_aimViews == null)
            {
                return;
            }

            for (var i = 0; i < _aimViews.Count; i++)
            {
                _aimViews[i].LookAimPosition(GetAimPosition(i == 0));
            }
        }

        public Vector3 GetAimPosition(bool isLeft)
        {
            return _topdownViews[isLeft ? 0 : 1].GetAimPosition();
        }

        public bool SetFocus(bool isLeft)
        {
            return false;
            //return _topdownViews[isLeft ? 0 : 1].IsFocus();
        }

        public void Shot(bool isLeft)
        {
            _shotViews[isLeft ? 0 : 1].SpawnBullet();
        }
    }
}