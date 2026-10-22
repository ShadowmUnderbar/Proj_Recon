using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IAfterimagePresenter
    {
        /// <summary>プレイヤーの今の姿で残像を1体残す</summary>
        void SpawnPlayerAfterimage(AfterimageStyle style);
    }
}
