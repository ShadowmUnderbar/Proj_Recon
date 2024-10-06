using App.Battle.Interface;
using System.Collections.Generic;
using App.Battle.Data;
using UnityEngine;
using R3;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour, IPlayerTopDownAimStoreView
    {
        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();
        public Observable<int> OnFocus => _onFocus;
        private readonly Subject<int> _onFocus = new();
        public Observable<int> OnUnFocus => _onUnFocus;
        private readonly Subject<int> _onUnFocus = new();

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

            foreach (var view in _shotViews)
            {
                view.OnHit.Subscribe(x=> _onHit.OnNext(x)).AddTo(this);
            }

            for(var i = 0;i< _topdownViews.Count;i++)
            {
                _topdownViews[i].OnFocus.Subscribe(_=> _onFocus.OnNext(i)).AddTo(this);
                _topdownViews[i].OnUnFocus.Subscribe(_=> _onUnFocus.OnNext(i)).AddTo(this);
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

        public void Shot(bool isLeft)
        {
            _shotViews[isLeft ? 0 : 1].SpawnBullet();
        }
    }
}