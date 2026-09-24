// レイ・トレーサー・弾の軌跡など、LineRenderer / TrailRenderer 向けのカーブ対応シェーダ。
// 陰影を持たず、頂点カラー（LineRendererのグラデーション）を乗せて半透明で描く。
//
// 頂点が無い場所は曲がらない。2点しか持たないLineRendererにこれを当てると
// 両端だけが沈んで間は直線のまま残るため、線を引く側で十分に刻んでおく必要がある。
// 刻む間隔は CurvedWorldConfig.MaxLineSegmentLength が出す。
Shader "App/CurvedWorldUnlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [Toggle(_CURVEDWORLD_PER_OBJECT)] _CurvedWorldPerObject("ピボット基準で沈める", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma shader_feature_local_vertex _CURVEDWORLD_PER_OBJECT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CurvedWorld.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _CurvedWorldPerObject;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                CurvedWorld_Vertex(positionWS, normalWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor * input.color;
                color.rgb += _EmissionColor.rgb * color.a;

                // MixFogはrgbをフォグ色へ寄せるだけでアルファは変えない。
                // 遠方のレイを薄れさせたい場合はアルファ側も落とす必要がある
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a *= input.fogFactor;
                return color;
            }
            ENDHLSL
        }
    }
}
