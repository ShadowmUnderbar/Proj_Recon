using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimStoreView
    {
        void InitStoreView(
            IPlayerTopDownAimView leftTopDown, IPlayerTopDownAimView rightTopDown,
            IPlayerAimMuzzleView leftAim, IPlayerAimMuzzleView rightAim,
            IPlayerShotView leftShot, IPlayerShotView rightShot);

        Observable<HitData> OnHit { get; }

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }

        void Aim();
        void Shot(ShotType shotType, bool isLeft);
    }
}