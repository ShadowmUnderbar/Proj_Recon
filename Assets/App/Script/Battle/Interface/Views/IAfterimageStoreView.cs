using App.Battle.Data;

namespace App.Battle.Interface
{
    /// <summary>残像の生成・寿命・描画をまとめて持つ</summary>
    public interface IAfterimageStoreView
    {
        /// <summary>
        /// <paramref name="source"/> の今の姿で残像を1体残す。形状の取得はアニメーションが反映された後（同じフレームの LateUpdate）に行う
        /// </summary>
        void Spawn(IAfterimageSource source, AfterimageStyle style);
    }
}
