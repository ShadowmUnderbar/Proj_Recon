Shader "Custom/DepthClipLit"
{
    Properties
    {
        _BaseMap("Albedo (RGB)", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ────────────────────────────────────────────
        // ForwardLit: デプスクリップ付きPBRライティング
        // ────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _Smoothness;
                half   _Metallic;
            CBUFFER_END

            // DepthClipperController.cs から Shader.SetGlobalFloat で設定
            // 0 のときはクリッピング無効
            float _ClipperDepth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                // クリップ空間の W = リニアアイ深度（頂点シェーダー時点の値を補間で渡す）
                float  eyeDepth   : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs    = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normalInputs.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
                // positionCS.w は頂点シェーダー時点ではリニアアイ深度（カメラからの距離）
                output.eyeDepth   = posInputs.positionCS.w;
                UNITY_TRANSFER_FOG(output, output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ── デプスクリップ判定 ──
                // _ClipperDepth より前面（カメラに近い）フラグメントを非表示
                if (_ClipperDepth > 0.0 && input.eyeDepth < _ClipperDepth)
                    discard;

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                InputData inputData = (InputData)0;
                inputData.positionWS            = input.positionWS;
                inputData.normalWS              = normalize(input.normalWS);
                inputData.viewDirectionWS       = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.fogCoord              = input.fogFactor;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo.rgb;
                surfaceData.alpha      = albedo.a;
                surfaceData.metallic   = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS   = half3(0, 0, 1);
                surfaceData.emission   = _EmissionColor.rgb;
                surfaceData.occlusion  = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ────────────────────────────────────────────
        // ShadowCaster: 影は通常通り投影
        // ────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _Smoothness;
                half   _Metallic;
            CBUFFER_END

            half _Cutoff; // ShadowCasterPass.hlsl が参照するため宣言

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ────────────────────────────────────────────
        // DepthOnly: カメラデプステクスチャ生成用
        // ────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _Smoothness;
                half   _Metallic;
            CBUFFER_END

            half _Cutoff;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
