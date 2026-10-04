using System;
using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// セピア調の強さ・色・グループのビット対応を、すべてのシェーダへグローバル変数として配る。
    /// 受け取る側は SepiaTone.hlsl。グループの付け方は <see cref="SepiaToneTargetView"/> を参照。
    /// </summary>
    public class SepiaToneView : ISepiaToneView, IDisposable
    {
        private static readonly int WeightsId = Shader.PropertyToID("_SepiaToneWeights");
        private static readonly int LayerBitsId = Shader.PropertyToID("_SepiaToneLayerBits");
        private static readonly int ColorId = Shader.PropertyToID("_SepiaToneColor");

        private readonly SepiaToneConfig _config;

        public SepiaToneView(SepiaToneConfig config)
        {
            _config = config;
        }

        public void SetWeights(Vector4 weights)
        {
            // 色は実機で調整しながら見たいので、強さと一緒に毎回配り直す（変化したときにしか呼ばれない）
            Shader.SetGlobalVector(LayerBitsId, LayerBits());
            Shader.SetGlobalColor(ColorId, _config.ToneColor);
            Shader.SetGlobalVector(WeightsId, weights);
        }

        public void Dispose()
        {
            // グローバル変数はシーンをまたいで残る（エディタでは再生終了後も残る）ので、効果なしに戻してから終える
            Shader.SetGlobalVector(WeightsId, Vector4.zero);
        }

        /// <summary>グループごとの renderingLayerMask のビット値。float で正確に表せる範囲（2^24以下）に収まっている</summary>
        private static Vector4 LayerBits()
        {
            var bits = Vector4.zero;
            for (var i = 0; i < SepiaToneRenderingLayer.GroupCount; i++)
            {
                bits[i] = SepiaToneRenderingLayer.LayerBit(i);
            }

            return bits;
        }
    }
}
