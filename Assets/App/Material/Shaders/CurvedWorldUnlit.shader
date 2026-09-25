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

        // 既存マテリアルの設定をそのまま引き継ぐための隠しプロパティ。
        // URPのParticlesUnlitと同じ名前・同じ既定値にしてあるので、
        // シェーダを差し替えても不透明・半透明のどちらの設定も保たれる
        [HideInInspector] _SrcBlend("__src", Float) = 5
        [HideInInspector] _DstBlend("__dst", Float) = 10
        [HideInInspector] _ZWrite("__zw", Float) = 0
        [HideInInspector] _Cull("__cull", Float) = 2
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

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

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
                // 描画状態の指定に使うだけでHLSLからは読まないが、
                // SRP Batcherの対象になるにはCBUFFERに並べておく必要がある
                half _SrcBlend;
                half _DstBlend;
                half _ZWrite;
                half _Cull;
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

                // MixFogはrgbをフォグ色へ寄せるだけで、アルファには触らない。
                // 遠方のレイは薄くならずフォグ色に染まる。
                // アルファも落としたくなるが、フォグ無効時は fogFactor が0になるため
                // 単純に掛けると全て透明になる。落とすならフォグのキーワードで分岐すること
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
        // 不透明設定で使われるマテリアル（EnemyBulletなど）のために、
        // デプス・デプス法線・影のパスも持たせる。
        // これらを欠くと、デプスプリパスやSSAOを有効にしたときに破綻し、影も出なくなる。
        // 半透明マテリアルではURP側がこれらのパスを呼ばないため、持っていても無害。
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite [_ZWrite]
            Cull [_Cull]
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local_vertex _CURVEDWORLD_PER_OBJECT
            #pragma multi_compile_instancing
            #include "CurvedWorldUnlitDepth.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentNormals
            #pragma shader_feature_local_vertex _CURVEDWORLD_PER_OBJECT
            #pragma multi_compile_instancing
            #include "CurvedWorldUnlitDepth.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex VertexShadow
            #pragma fragment Fragment
            #pragma shader_feature_local_vertex _CURVEDWORLD_PER_OBJECT
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "CurvedWorldUnlitDepth.hlsl"
            ENDHLSL
        }
    }
}
