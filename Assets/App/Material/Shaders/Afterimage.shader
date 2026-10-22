// プレイヤーの残像用シェーダ。元のシルエットと陰影だけを残し、指定色の単色で塗って、
// 表面をブロック状に欠けさせる（データが不完全に再構築されたような見た目）。
//
// ・色: 元テクスチャの色相は使わない。ライティングの明暗（と、薄くテクスチャの明暗）だけを取り出し、
//   指定色の「暗部〜ハイライト」のランプへ当てる。元の色と残像色を掛け合わせないので濁らない
// ・欠損: 残像の基準点からの相対座標（ワールド軸）でブロックに区切ったノイズを、しきい値で Alpha Clip する。
//   体と頭で同じ基準点を使うので、パーツの境目で模様が途切れない。スクリーン座標は使わない
// ・不透明度（全体のフェード）と欠損量（表面のマスク）は別のプロパティで独立に効く
// ・半透明だが、先に深度だけを書くパスを持つ。腕と胴の重なりや体と頭の重なりで、奥の面が透けて二重に見えない
//
// 値はインスタンスごとに MaterialPropertyBlock で渡す（AfterimageDrawer）。
// 水平線カーブは基準点（_AfterimagePivot）でまとめて沈め、体と頭の位置関係を崩さない。
Shader "App/Afterimage"
{
    Properties
    {
        _BaseMap("元テクスチャ（明暗だけ使う）", 2D) = "white" {}
        _Color("残像の色", Color) = (0.3, 0.85, 1, 1)
        _Saturation("彩度", Range(0, 1)) = 0.8
        _Opacity("不透明度", Range(0, 1)) = 0.7
        _ShadeContrast("陰影のコントラスト", Range(0, 4)) = 1.2
        _Dissolve("欠損量", Range(0, 1)) = 0.15
        _NoiseScale("欠損模様の細かさ（1mあたりのブロック数）", Float) = 12
        _NoiseSpeed("欠損模様が組み替わる速さ（回/秒）", Float) = 8
        [HDR] _EdgeColor("切り口の色", Color) = (0.6, 1.6, 2, 1)
        _EdgeWidth("切り口の幅", Range(0, 0.5)) = 0.08
        _TextureShade("元テクスチャの明暗を陰影に混ぜる量", Range(0, 1)) = 0.3
        _ShadowBrightness("暗部の明るさ（残像色に対する倍率）", Range(0, 1)) = 0.25
        _HighlightBoost("ハイライトを白へ寄せる量", Range(0, 1)) = 0.35
        _RimStrength("輪郭の光", Range(0, 2)) = 0.6

        // 以下は AfterimageDrawer がインスタンスごとに渡す
        [HideInInspector] _AfterimagePivot("基準点（ワールド）", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AfterimageSeed("模様の乱数の種", Float) = 0
        [HideInInspector] _AfterimageAge("生成からの経過秒数", Float) = 0
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CurvedWorld.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Color;
            half _Saturation;
            half _Opacity;
            half _ShadeContrast;
            half _Dissolve;
            float _NoiseScale;
            float _NoiseSpeed;
            half4 _EdgeColor;
            half _EdgeWidth;
            half _TextureShade;
            half _ShadowBrightness;
            half _HighlightBoost;
            half _RimStrength;
            float4 _AfterimagePivot;
            float _AfterimageSeed;
            float _AfterimageAge;
        CBUFFER_END

        // 明暗の素の組み立て。半ランバートとその2乗の中間を取り、暗部側を少し締める
        #define AFTERIMAGE_LAMBERT_SQUARE_BLEND 0.5
        // 明暗に対する環境光（SH）の寄与。大半は光の向きで決め、真裏が真っ黒にならない程度に足す
        #define AFTERIMAGE_AMBIENT_WEIGHT 0.15
        // 陰影のコントラストの軸になる明るさ
        #define AFTERIMAGE_SHADE_PIVOT 0.5
        // 輪郭の光の鋭さ（大きいほど縁の細い範囲だけが光る）
        #define AFTERIMAGE_RIM_POWER 3.0

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 normalWS : TEXCOORD2;
            // 欠損模様を引く座標。基準点からの相対位置（カーブで沈める前）
            float3 patternPosition : TEXCOORD3;
            float fogFactor : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings AfterimageVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.patternPosition = positionWS - _AfterimagePivot.xyz;

            // 体と頭で別々のピボットを使うと沈む量がずれるため、残像共通の基準点で剛体のまま沈める
            positionWS = CurvedWorld_ApplyPerObject(positionWS, _AfterimagePivot.xyz);

            output.positionWS = positionWS;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.positionCS = TransformWorldToHClip(positionWS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.fogFactor = ComputeFogFactor(output.positionCS.z);
            return output;
        }

        // 整数ハッシュ（PCG系）。frac(sin) はモバイルの精度で縞が出るため使わない
        uint AfterimageHash(uint3 v)
        {
            v = v * 1664525u + 1013904223u;
            v.x += v.y * v.z;
            v.y += v.z * v.x;
            v.z += v.x * v.y;
            v ^= v >> 16u;
            v.x += v.y * v.z;
            return v.x;
        }

        float AfterimageHash01(int3 cell, uint salt)
        {
            uint3 v = (uint3)cell + uint3(salt, salt * 7919u, salt * 104729u);
            return (AfterimageHash(v) & 0x00FFFFFFu) / 16777216.0;
        }

        // 欠損の判定値（0〜1）。ブロックごとに乱数を持ち、時間で隣の乱数へなめらかに移る
        float AfterimagePattern(float3 patternPosition)
        {
            int3 cell = (int3)floor(patternPosition * _NoiseScale);
            float time = _AfterimageAge * _NoiseSpeed;
            uint step = (uint)floor(time);
            uint seed = (uint)_AfterimageSeed;
            float current = AfterimageHash01(cell, seed + step);
            float next = AfterimageHash01(cell, seed + step + 1u);
            return lerp(current, next, smoothstep(0.0, 1.0, frac(time)));
        }
        ENDHLSL

        // 深度だけを先に書く。欠損で抜けた所は書かないので、穴の奥は次のパスで見える
        Pass
        {
            Name "AfterimageDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex AfterimageVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                clip(AfterimagePattern(input.patternPosition) - _Dissolve);
                clip(_Opacity - 0.001);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex AfterimageVertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SepiaTone.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 欠損。全体のフェード（_Opacity）とは別に、表面をしきい値で切り取る
                float pattern = AfterimagePattern(input.patternPosition);
                float remain = pattern - _Dissolve;
                clip(remain);
                half edge = _EdgeWidth > 0.0001 ? 1.0 - saturate(remain / _EdgeWidth) : 0.0;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // 立体感の素になる明暗（0〜1）。光の色や強さには左右させず、向きだけを使う。
                // 半ランバートにして、光の当たらない側もつぶさず形を残す
                Light mainLight = GetMainLight();
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                half ambient = saturate(Luminance(SampleSH(normalWS)));
                half lambertTone = lerp(halfLambert, halfLambert * halfLambert, AFTERIMAGE_LAMBERT_SQUARE_BLEND);
                half tone = lerp(lambertTone, ambient, AFTERIMAGE_AMBIENT_WEIGHT);

                // 元テクスチャは色相を捨て、明暗だけを薄く混ぜる（細部より形を優先する）
                half textureLuma = Luminance(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb);
                tone *= lerp(1.0, textureLuma, _TextureShade);

                // 陰影のコントラスト。中間の明るさを軸に広げる・縮める
                half shade = saturate((tone - AFTERIMAGE_SHADE_PIVOT) * _ShadeContrast + AFTERIMAGE_SHADE_PIVOT);

                // 残像色の暗部〜ハイライトのランプに当てる。元の色と残像色を掛け合わせないので濁らない
                half3 baseColor = lerp(Luminance(_Color.rgb).xxx, _Color.rgb, _Saturation);
                half3 shadowColor = baseColor * _ShadowBrightness;
                half3 highlightColor = lerp(baseColor, half3(1, 1, 1), _HighlightBoost);
                half3 color = lerp(shadowColor, highlightColor, shade);

                // 輪郭を光らせてシルエットを立てる（見下ろしでも体の縁が埋もれないように）
                half rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), AFTERIMAGE_RIM_POWER) * _RimStrength;
                color += baseColor * rim;

                // 欠損の切り口を縁取る
                color += _EdgeColor.rgb * edge;

                color = MixFog(color, input.fogFactor);
                color = SepiaTone_Apply(color, SepiaTone_RendererWeight());
                return half4(color, _Opacity);
            }
            ENDHLSL
        }
    }
}
