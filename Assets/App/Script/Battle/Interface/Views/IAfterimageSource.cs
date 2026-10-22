using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>
    /// 残像の元になる見た目（体と、いま付いている頭）を差し出す。
    /// 頭の差し替えの仕組みには関与せず、呼ばれた時点で表示されている Renderer をそのまま返す。
    /// </summary>
    public interface IAfterimageSource
    {
        /// <summary>
        /// 残像にする Renderer（SkinnedMeshRenderer / MeshRenderer）を <paramref name="results"/> へ足す。
        /// 1つ目は体。残像全体の基準点（欠損模様・水平線カーブの基準）に使う
        /// </summary>
        void CollectRenderers(List<Renderer> results);

        /// <summary>体の向いている水平方向（正規化済み）。残像を後ろへずらす向きに使う</summary>
        Vector3 Forward { get; }
    }
}
