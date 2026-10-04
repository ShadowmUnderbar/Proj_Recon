using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>セピア調の強さをシェーダへ配る</summary>
    public interface ISepiaToneView
    {
        /// <summary>グループごとのセピアの強さ（0〜1）を配る。x:背景 y:敵 z:プレイヤー w:UI</summary>
        void SetWeights(Vector4 weights);
    }
}
