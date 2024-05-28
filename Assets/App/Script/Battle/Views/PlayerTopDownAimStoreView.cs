using App.Battle.Interface.Views;
using App.Framework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour , IPlayerTopDownAimStoreView
    {
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
            return _topdownViews[isLeft ? 0 : 1].GetAimPosition();
        }

        public bool SetFocus(bool isLeft)
        {
            return _topdownViews[isLeft ? 0 : 1].IsFocus();
        }
        public void Shot(bool isLeft)
        {
            _shotViews[isLeft ? 0 : 1].SpawnBullet();
        }
    }
}