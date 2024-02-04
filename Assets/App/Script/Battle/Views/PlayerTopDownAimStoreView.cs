using App.Battle.Interface.View;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerTopDownAimStoreView : MonoBehaviour , IPlayerTopDownAimStoreView
    {
        [SerializeField]
        private PlayerTopDownAimView[] _playerTopDownAimViews;
        [SerializeField]
        private PlayerAimMuzzleView[] _playerAimMuzzleView;

        public void Aim()
        {
            for(var i = 0;i< _playerAimMuzzleView.Length;i++)
            {
                _playerAimMuzzleView[i].LookAimPosition(GetAimPosition(i == 0));
            }
        }


        public Vector3 GetAimPosition(bool isLeft)
        {
            return _playerTopDownAimViews[isLeft ? 0 : 1].GetAimPosition();
        }
    }
}