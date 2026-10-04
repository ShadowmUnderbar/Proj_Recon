#ifndef SEPIA_TONE_INCLUDED
#define SEPIA_TONE_INCLUDED

// 描画対象をグループ（背景 / 敵 / プレイヤー / UI）に分け、グループごとにセピア調へ寄せる共通処理。
// 値はすべて SepiaToneView が毎フレーム配るグローバル変数で、マテリアルには持たせない。
// 未設定（エディタの非再生中など）では強さが0になり、元の色のまま描かれる。
//
// どのグループに属するかは、Renderer の renderingLayerMask の特定ビットで表す
// （SepiaToneTargetView が立てる）。マテリアルではなく Renderer 側に持たせるので、
// 敵とプレイヤーで同じマテリアルを共有しても、それぞれ別のグループとして扱える。
// renderingLayerMask は UnityPerDraw に入るため、SRP Batcher のまとめ描きも崩さない。
// uGUI は Renderer を持たないので、UI 用シェーダはマテリアルのプロパティでグループを選ぶ。

// グループごとのセピアの強さ（0〜1）。x:背景 y:敵 z:プレイヤー w:UI
float4 _SepiaToneWeights;
// グループごとの renderingLayerMask のビット値（2のべき乗を float で渡す）。並びは _SepiaToneWeights と同じ
float4 _SepiaToneLayerBits;
// 輝度に掛けてセピアの色にする係数。a は使わない
float4 _SepiaToneColor;

// Rec.601 の輝度係数。セピア写真の見た目に合わせて、知覚輝度ではなく古典的な係数を使う
#define SEPIA_TONE_LUMA half3(0.299, 0.587, 0.114)

// この Renderer が属するグループのセピアの強さ。複数のビットが立っていれば強いほうを使う
half SepiaTone_RendererWeight()
{
    uint mask = asuint(unity_RenderingLayer.x);
    uint4 bits = (uint4)_SepiaToneLayerBits;
    float4 hit = float4((mask & bits) != 0);
    float4 weights = hit * _SepiaToneWeights;
    return (half)max(max(weights.x, weights.y), max(weights.z, weights.w));
}

// グループ番号（0:背景 1:敵 2:プレイヤー 3:UI）からセピアの強さを引く。UI 用
half SepiaTone_GroupWeight(float group)
{
    return (half)_SepiaToneWeights[(uint)group];
}

// 色を強さに応じてセピアへ寄せる。HDR の値も輝度として扱い、明るさは保つ
half3 SepiaTone_Apply(half3 color, half weight)
{
    half luma = dot(color, SEPIA_TONE_LUMA);
    half3 sepia = luma * (half3)_SepiaToneColor.rgb;
    return lerp(color, sepia, weight);
}

#endif
