// ステージ遷移用の床崩壊シェーダ。演出用のプレート床（FloorCollapsePlateMeshBuilder が組む、プレートごとに頂点を分けた板）に使う。
//
// 全体の進行度 _CollapseProgress 1つから、プレートごとに次を決める。
// ・崩壊の波: 中心からの距離（または一方向への距離）に、プレートごとの乱数のずれを足した位置を波が通ると、そのプレートが動き出す
// ・亀裂: 動き出す前の区間（_CrackPortion）は、境界の線が区切りごとに順に光るだけ
// ・変形: プレート単位の剛体変換として、隙間を開けながら沈下・外側の縁が下がる傾斜・外側へのスライドを重ねる
// ・データ片化: 動きの後半でプレートをブロック状に割って Alpha Clip で消し、切り口を光らせる
// ・暗転: 全体の進行度の終盤で、色と発光を黒へ落とす
// 床の見た目（テクスチャと色）は元の床から渡され、真上からの投影で貼る。元の床のUV・法線は使わない。
// 不透明＋Alpha Clip なので、透明のオーバードローは増えない。
// 全パスに同じ変形と水平線カーブ（頂点ごと）を通す。どれかが欠けると、そのパスだけ元の位置に残る
Shader "App/FloorCollapse"
{
    Properties
    {
        _BaseMap("床のテクスチャ（元の床から渡す）", 2D) = "white" {}
        _BaseColor("床の色（元の床から渡す）", Color) = (1, 1, 1, 1)
        _PlanarRect("投影の範囲（xz最小, xzの大きさ）", Vector) = (-300, -300, 600, 600)
        [HDR] _GlowColor("亀裂・切り口の発光色", Color) = (0.4, 1.8, 2.4, 1)
        _LineWidth("境界の発光線の幅[m]", Float) = 0.06
        _SideDarkness("断面の明るさ（上面に対する倍率）", Range(0, 1)) = 0.3

        [Header(Progress)]
        _CollapseProgress("全体の進行度", Range(0, 1)) = 0
        _CollapseOrigin("崩壊の中心（オブジェクト空間）", Vector) = (0, 0, 0, 0)
        _CollapseDirection("向き（xy）と進み方（z: 0=同心円 1=一方向）", Vector) = (0, 1, 0, 0)
        _MaxTravel("波が進む最大距離[m]", Float) = 64
        _Stagger("1枚が動き切るまでに波が進む距離[m]", Float) = 12
        _Jitter("プレートごとの開始のずれ[m]", Float) = 3
        _CrackPortion("亀裂だけの区間", Range(0, 0.9)) = 0.3

        [Header(Motion)]
        _HalfPlateSize("プレートの一辺の半分[m]", Float) = 1.25
        _SinkDepth("沈下量[m]", Float) = 12
        _TiltAngle("傾斜の角度[rad]", Float) = 0.6
        _SlideDistance("外側へのスライド量[m]", Float) = 3
        _GapRatio("隙間の割合", Range(0, 0.5)) = 0.12

        [Header(Dissolve)]
        _DissolveStart("データ片化の開始", Range(0, 1)) = 0.45
        _FragmentsPerPlate("1枚を一辺何個に割るか", Float) = 5
        _BlackoutStart("暗転の開始", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CurvedWorld.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float4 _PlanarRect;
            half4 _GlowColor;
            float _LineWidth;
            half _SideDarkness;
            float _CollapseProgress;
            float4 _CollapseOrigin;
            float4 _CollapseDirection;
            float _MaxTravel;
            float _Stagger;
            float _Jitter;
            float _CrackPortion;
            float _HalfPlateSize;
            float _SinkDepth;
            float _TiltAngle;
            float _SlideDistance;
            float _GapRatio;
            float _DissolveStart;
            float _FragmentsPerPlate;
            float _BlackoutStart;
        CBUFFER_END

        // 傾斜の強さのばらつき（乱数0〜1に対する倍率の下限）。全部が同じ角度で倒れると機械的に見える
        #define FLOOR_COLLAPSE_MIN_VARIATION 0.6
        // 隙間が開ききるまでの、動きの進み（0〜1）
        #define FLOOR_COLLAPSE_GAP_OPEN 0.25
        // 亀裂が区切りごとに順に走るときの、境界1辺の区切り数
        #define FLOOR_COLLAPSE_CRACK_SEGMENTS 4.0
        // 亀裂の明滅の速さ（回/秒）
        #define FLOOR_COLLAPSE_FLICKER_RATE 18.0
        // データ片の切り口を光らせる幅（しきい値からの差）
        #define FLOOR_COLLAPSE_FRAGMENT_EDGE 0.08
        // 亀裂の明滅で暗くなるときの明るさ（1で明滅しない）
        #define FLOOR_COLLAPSE_FLICKER_MIN 0.6
        // 落ち切ったときに亀裂の発光を落とす割合（割れ始めがいちばん強く光る）
        #define FLOOR_COLLAPSE_GLOW_FADE 0.5
        // 断面の上端の縁を光らせる幅（境界の発光線の幅に対する倍率）
        #define FLOOR_COLLAPSE_SIDE_RIM_SCALE 2.0

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            // xy: プレートの中心（オブジェクト空間のxz） zw: プレートごとの乱数
            float4 plateData : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            // xyz: プレート中心からの位置（変形前） w: 断面なら1
            float4 plateLocal : TEXCOORD2;
            // x: 亀裂の進み y: 動きの進み z: データ片化の進み w: プレートの乱数
            float4 phase : TEXCOORD3;
            // xy: 投影のUV（変形前のワールド位置から） z: フォグ
            float3 planarUVFog : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // 単位ベクトル axis 周りに angle 回す（ロドリゲスの回転公式）
        float3 FloorCollapse_Rotate(float3 v, float3 axis, float angle)
        {
            float s, c;
            sincos(angle, s, c);
            return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
        }

        // プレートの崩壊の波からの位置（0で波が届いたばかり、大きいほど先に崩れている）[m]
        float FloorCollapse_Distance(float2 center)
        {
            float2 fromOrigin = center - _CollapseOrigin.xz;
            float radial = length(fromOrigin);
            // 一方向のときは、範囲の向きの逆側の端を0とする距離（_MaxTravel は範囲を向きへ射影した幅）
            float directional = dot(fromOrigin, _CollapseDirection.xy) + _MaxTravel * 0.5;
            return lerp(radial, directional, _CollapseDirection.z);
        }

        // プレートがずれていく外向き（xz）
        float2 FloorCollapse_Outward(float2 center)
        {
            float2 fromOrigin = center - _CollapseOrigin.xz;
            float len = length(fromOrigin);
            float2 radial = len > 0.0001 ? fromOrigin / len : float2(1, 0);
            return normalize(lerp(radial, _CollapseDirection.xy, _CollapseDirection.z));
        }

        Varyings FloorCollapseVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float2 center2 = input.plateData.xy;
            float3 center = float3(center2.x, 0, center2.y);
            float random = input.plateData.z;
            float random2 = input.plateData.w;
            float3 local = input.positionOS.xyz - center;

            // 波の位置。最遠のプレートが動き切ったときに全体の進行度の暗転開始に届くよう伸ばす
            float travel = _MaxTravel + _Stagger + _Jitter;
            float front = _CollapseProgress / max(_BlackoutStart, 0.0001) * travel;
            float plate = saturate((front - FloorCollapse_Distance(center2) - random * _Jitter) / _Stagger);

            float crack = saturate(plate / max(_CrackPortion, 0.0001));
            float motion = saturate((plate - _CrackPortion) / max(1.0 - _CrackPortion, 0.0001));
            // 落ちるにつれて加速させる
            float fall = motion * motion;
            float dissolve = saturate((motion - _DissolveStart) / max(1.0 - _DissolveStart, 0.0001));

            float2 outward2 = FloorCollapse_Outward(center2);
            float3 outward = float3(outward2.x, 0, outward2.y);
            // 外側の縁が下がる向きに倒す（up × outward）
            float3 tiltAxis = float3(outward.z, 0, -outward.x);
            float variation = lerp(FLOOR_COLLAPSE_MIN_VARIATION, 1.0, random2);

            // 隙間を開ける（プレートを中心へ向けて縮める）→ 傾ける → 沈めてずらす
            float3 shaped = local;
            shaped.xz *= 1.0 - _GapRatio * smoothstep(0.0, FLOOR_COLLAPSE_GAP_OPEN, motion);
            float angle = fall * _TiltAngle * variation;
            shaped = FloorCollapse_Rotate(shaped, tiltAxis, angle);
            float3 offset = outward * (_SlideDistance * fall * lerp(FLOOR_COLLAPSE_MIN_VARIATION, 1.0, random))
                          - float3(0, _SinkDepth * fall * variation, 0);
            float3 positionOS = center + shaped + offset;
            float3 normalOS = FloorCollapse_Rotate(input.normalOS, tiltAxis, angle);

            float3 positionWS = TransformObjectToWorld(positionOS);
            float3 normalWS = TransformObjectToWorldNormal(normalOS);
            CurvedWorld_Vertex(positionWS, normalWS);

            output.positionWS = positionWS;
            output.normalWS = normalWS;
            output.positionCS = TransformWorldToHClip(positionWS);
            output.plateLocal = float4(local, input.normalOS.y < 0.5 ? 1.0 : 0.0);
            output.phase = float4(crack, motion, dissolve, random);

            // 床の見た目は、変形前の位置を真上から投影して貼る（プレートが傾いても模様は板に貼り付いたまま）
            float3 restWS = TransformObjectToWorld(input.positionOS.xyz);
            float2 planarUV = (restWS.xz - _PlanarRect.xy) / _PlanarRect.zw;
            output.planarUVFog.xy = planarUV * _BaseMap_ST.xy + _BaseMap_ST.zw;
            output.planarUVFog.z = ComputeFogFactor(output.positionCS.z);
            return output;
        }

        uint FloorCollapse_Hash(uint3 v)
        {
            v = v * 1664525u + 1013904223u;
            v.x += v.y * v.z;
            v.y += v.z * v.x;
            v.z += v.x * v.y;
            v ^= v >> 16u;
            v.x += v.y * v.z;
            return v.x;
        }

        float FloorCollapse_Hash01(int3 cell, float salt)
        {
            uint3 v = (uint3)cell + (uint)(salt * 65536.0) * uint3(1u, 7919u, 104729u);
            return (FloorCollapse_Hash(v) & 0x00FFFFFFu) / 16777216.0;
        }

        // データ片に割ったときの、この点の片の乱数（0〜1）。しきい値より小さい片から消える
        float FloorCollapse_Fragment(float4 plateLocal, float random)
        {
            float cellSize = (_HalfPlateSize * 2.0) / max(_FragmentsPerPlate, 1.0);
            int3 cell = (int3)floor((plateLocal.xyz + _HalfPlateSize) / cellSize);
            return FloorCollapse_Hash01(cell, random);
        }

        // データ片化で抜ける部分を切り取り、切り口の近さ（0〜1）を返す
        half FloorCollapse_ClipFragment(Varyings input)
        {
            float dissolve = input.phase.z;
            float fragment = FloorCollapse_Fragment(input.plateLocal, input.phase.w);
            float remain = fragment - dissolve;
            // 動き切ったプレートは残さない（乱数がちょうど1の片も消す）
            clip(dissolve >= 1.0 ? -1.0 : remain);
            return dissolve > 0.0 ? 1.0 - saturate(remain / FLOOR_COLLAPSE_FRAGMENT_EDGE) : 0.0;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back

            HLSLPROGRAM
            #pragma vertex FloorCollapseVertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
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

                half fragmentEdge = FloorCollapse_ClipFragment(input);

                float crack = input.phase.x;
                float motion = input.phase.y;
                float random = input.phase.w;
                bool isSide = input.plateLocal.w > 0.5;

                // 床の色。断面は暗くする
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.planarUVFog.xy).rgb * _BaseColor.rgb;
                albedo *= isSide ? _SideDarkness : 1.0;

                // 陰影（傾いたプレートの面の向きで明暗が変わる）。影は動いた後の位置で受ける
                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lighting = mainLight.color * (saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation)
                               + SampleSH(normalWS);
                half3 color = albedo * lighting;

                // 境界の亀裂。辺を区切りに分け、亀裂の進みに応じて区切りごとに順に光らせる
                float2 local = input.plateLocal.xz;
                float2 absLocal = abs(local);
                float edgeDistance = _HalfPlateSize - max(absLocal.x, absLocal.y);
                half edgeLine = 1.0 - smoothstep(0.0, _LineWidth, edgeDistance);
                float along = absLocal.x > absLocal.y ? local.y : local.x;
                int3 segmentCell = int3(floor((along / _HalfPlateSize + 1.0) * FLOOR_COLLAPSE_CRACK_SEGMENTS),
                                        absLocal.x > absLocal.y ? (local.x > 0 ? 1 : -1) : 0,
                                        absLocal.x > absLocal.y ? 0 : (local.y > 0 ? 1 : -1));
                half segmentReveal = step(FloorCollapse_Hash01(segmentCell, random), crack);
                // 断面は上端の縁だけを光らせる
                half sideRim = isSide ? 1.0 - smoothstep(0.0, _LineWidth * FLOOR_COLLAPSE_SIDE_RIM_SCALE, -input.plateLocal.y) : 0.0;
                half flicker = lerp(FLOOR_COLLAPSE_FLICKER_MIN, 1.0, FloorCollapse_Hash01(int3(floor(_Time.y * FLOOR_COLLAPSE_FLICKER_RATE), 0, 0), random));
                // 割れ始めは強く光り、落ちていくにつれて落ち着かせる
                half crackGlow = max(isSide ? sideRim : edgeLine * segmentReveal, 0.0) * crack * flicker * (1.0 - motion * FLOOR_COLLAPSE_GLOW_FADE);
                half3 emission = _GlowColor.rgb * max(crackGlow, fragmentEdge);

                // 暗転。全体の進行度の終盤で色も発光も黒へ落とす
                half blackout = saturate((_CollapseProgress - _BlackoutStart) / max(1.0 - _BlackoutStart, 0.0001));
                color = (color + emission) * (1.0 - blackout);

                color = MixFog(color, input.planarUVFog.z);
                color = SepiaTone_Apply(color, SepiaTone_RendererWeight());
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output = FloorCollapseVertex(input);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - output.positionWS);
#else
                float3 lightDirectionWS = _LightDirection;
#endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, normalize(output.normalWS), lightDirectionWS));
#if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
                output.positionCS = positionCS;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                FloorCollapse_ClipFragment(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex FloorCollapseVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                FloorCollapse_ClipFragment(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex FloorCollapseVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                FloorCollapse_ClipFragment(input);
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
