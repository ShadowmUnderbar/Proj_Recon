#ifndef CURVED_WORLD_UNLIT_DEPTH_INCLUDED
#define CURVED_WORLD_UNLIT_DEPTH_INCLUDED

// CurvedWorldUnlit のデプス・デプス法線・影のパスで共有する頂点処理。
// 不透明設定で使うマテリアルはこれらのパスを通るため、
// ForwardLitと同じ変位を掛けないと、そのパスだけ元の位置に残る。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "CurvedWorld.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct DepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

DepthVaryings Vertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    CurvedWorld_Vertex(positionWS, normalWS);

    output.positionCS = TransformWorldToHClip(positionWS);
    output.normalWS = normalWS;
    return output;
}

DepthVaryings VertexShadow(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    CurvedWorld_Vertex(positionWS, normalWS);

#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    output.positionCS = positionCS;
    output.normalWS = normalWS;
    return output;
}

half4 Fragment(DepthVaryings input) : SV_Target
{
    return 0;
}

half4 FragmentNormals(DepthVaryings input) : SV_Target
{
    return half4(normalize(input.normalWS), 0.0);
}

#endif
