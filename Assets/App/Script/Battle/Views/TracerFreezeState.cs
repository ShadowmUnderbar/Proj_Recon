using App.Battle.Interface;

namespace App.Battle.Views
{
    /// <summary>
    /// 即着弾のレイ演出の停止状態を保持する。
    /// フリーズの開始・解除に合わせて <c>FreezeUseCase</c> が書き換え、各レイが参照する。
    /// </summary>
    public class TracerFreezeState : ITracerFreezeState
    {
        public bool IsFreezing { get; private set; }

        public void SetFreezing(bool isFreezing)
        {
            IsFreezing = isFreezing;
        }
    }
}
