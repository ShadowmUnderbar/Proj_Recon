#ifndef CURVED_WORLD_INCLUDED
#define CURVED_WORLD_INCLUDED

// ワールドを水平線状に曲げるための共通処理。
// 沈下量をXZ平面上の距離だけの関数にしているため、垂直に立ったジオメトリ
// （壁の側面など）は全頂点のXZが等しく、同じ量だけ沈んで剛体のまま保たれる。
// 歪むのは水平方向に広がったジオメトリ（地面）だけになる。
//
// 沈下量に上限は設けない。途中で頭打ちにすると遠景が平らな面になり、
// その面が水平線の上へ折り返して「2枚目の床」として見えてしまう。
// カリング用バウンズをどれだけ広げるかは、C#側で別に持っている。

// x: 曲率。大きいほど手前で水平線が来る
float4 _CurvedWorldOrigin;
float4 _CurvedWorldParams;

#define CURVED_WORLD_STRENGTH _CurvedWorldParams.x

// 中心からのXZ距離に応じた沈下量[m]。放物面で球面を近似している
float CurvedWorld_Drop(float2 positionXZ)
{
    float2 delta = positionXZ - _CurvedWorldOrigin.xz;
    return dot(delta, delta) * CURVED_WORLD_STRENGTH;
}

// 沈下量のXZ方向の傾き。法線の補正に使う
float2 CurvedWorld_DropGradient(float2 positionXZ)
{
    float2 delta = positionXZ - _CurvedWorldOrigin.xz;
    return -2.0 * CURVED_WORLD_STRENGTH * delta;
}

// 頂点ごとに沈める。地面や、水平に広がった静的メッシュ向け
float3 CurvedWorld_ApplyPerVertex(float3 positionWS)
{
    positionWS.y -= CurvedWorld_Drop(positionWS.xz);
    return positionWS;
}

// オブジェクトのピボット基準でまるごと沈める。
// 腕や武器が横に張り出したキャラクタは、頂点ごとに沈めると距離差でせん断するため、
// 剛体のまま沈めるこちらを使う
float3 CurvedWorld_ApplyPerObject(float3 positionWS, float3 pivotWS)
{
    positionWS.y -= CurvedWorld_Drop(pivotWS.xz);
    return positionWS;
}

// 変形後の面に合わせて法線を倒す。ヤコビアンの逆転置を展開したもの
float3 CurvedWorld_ApplyNormal(float3 normalWS, float3 positionWS)
{
    float2 gradient = CurvedWorld_DropGradient(positionWS.xz);
    normalWS.xz -= gradient * normalWS.y;
    return normalize(normalWS);
}

// このオブジェクトのピボットのワールド座標
float3 CurvedWorld_PivotWS()
{
    return float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
}

// 頂点シェーダから呼ぶ入口。位置と法線をまとめて曲げる
void CurvedWorld_Vertex(inout float3 positionWS, inout float3 normalWS)
{
#if defined(_CURVEDWORLD_PER_OBJECT)
    positionWS = CurvedWorld_ApplyPerObject(positionWS, CurvedWorld_PivotWS());
#else
    normalWS = CurvedWorld_ApplyNormal(normalWS, positionWS);
    positionWS = CurvedWorld_ApplyPerVertex(positionWS);
#endif
}

#endif
