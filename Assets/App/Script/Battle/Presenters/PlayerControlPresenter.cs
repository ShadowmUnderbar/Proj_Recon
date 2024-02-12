using UnityEngine;
using App.Battle.Interface.Presenters;
using VContainer;
using App.Battle.Interface.Views;

namespace App.Battle.Presenters
{
    public class PlayerControlPresenter : IPlayerControlPresenter
    {
        private readonly IBattlePlayerView _battlePlayerView;

        [Inject]
        public PlayerControlPresenter(
            IBattlePlayerView battlePlayerView
        )
        {
            _battlePlayerView = battlePlayerView;
        }

        public void Move(Vector2 moveV2, float speed)
        {
            _battlePlayerView.Move(moveV2, speed);
        }

        public void Shot(bool IsLeft)
        {
            _battlePlayerView.Shot(IsLeft);
        }

        public void Aim()
        {
            _battlePlayerView.Aim();
        }
    }
}