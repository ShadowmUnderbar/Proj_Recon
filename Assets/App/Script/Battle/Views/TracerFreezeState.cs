using App.Battle.Interface;

namespace App.Battle.Views
{
    /// <summary>
    /// 即着弾のレイ演出の停止状態を保持する。
    /// フリーズは <c>FreezeUseCase</c>、オーバークロックは <c>OverclockUseCase</c> が書き換え、各レイが参照する。
    /// </summary>
    public class TracerFreezeState : ITracerFreezeState
    {
        private bool _isFreezeHold;

        public bool IsFreezing => _isFreezeHold || IsOverclock;

        public bool IsOverclock { get; private set; }

        public void SetFreezing(bool isFreezing)
        {
            _isFreezeHold = isFreezing;
        }

        public void SetOverclock(bool isOverclock)
        {
            IsOverclock = isOverclock;
        }
    }
}
