using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IAfterimagePresenter
    {
        /// <summary>プレイヤーの今の姿で残像を1体残す。<paramref name="backOffset"/>[m] だけ体の向きの真後ろへずらす</summary>
        void SpawnPlayerAfterimage(AfterimageStyle style, float backOffset);
    }
}
