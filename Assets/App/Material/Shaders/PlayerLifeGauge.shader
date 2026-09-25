// プレイヤーの足元に床置きする半円ライフゲージ。1枚のQuadのUVから弧を描く。
// UVの下半分（v < 0.5）に半円を描き、内側の太い弧がHP、外側の細い弧がバリア残量。
// 弧は左端→手前中央→右端の順に満たされ、HPが減ると右端側から欠けていく。
//
// 足元はカーブの中心（沈下量ほぼ0）なので見た目への影響はないが、
// 地面と同じ面に乗り続けるよう頂点ごとのカーブ変位は通しておく。
Shader "App/PlayerLifeGauge"
{
    Properties
    {
        _HealthColor("HPの色", Color) = (0.3, 1, 0.55, 0.9)
        _LowHealthColor("低HP時の色", Color) = (1, 0.25, 0.2, 0.95)
        _TrackColor("空き部分の色", Color) = (0, 0, 0, 0.4)
        _BarrierColor("バリアの色", Color) = (0.35, 0.8, 1, 0.9)

        _HealthInnerRadius("HP弧の内径", Range(0, 1)) = 0.74
        _HealthOuterRadius("HP弧の外径", Range(0, 1)) = 0.9
        _BarrierInnerRadius("バリア弧の内径", Range(0, 1)) = 0.93
        _BarrierOuterRadius("バリア弧の外径", Range(0, 1)) = 0.99

        _LowHealthThreshold("低HPとみなす割合", Range(0, 1)) = 0.3
        _BlinkFrequency("低HP時の点滅回数[回/秒]", Float) = 2
        _BlinkMinAlpha("点滅の最小アルファ", Range(0, 1)) = 0.3

        // Viewが MaterialPropertyBlock で毎回上書きする値
        [HideInInspector] _HealthFill("HP割合", Range(0, 1)) = 1
        [HideInInspector] _BarrierFill("バリア割合", Range(0, 1)) = 0
        [HideInInspector] _BarrierVisible("バリア表示", Float) = 0
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
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CurvedWorld.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HealthColor;
                half4 _LowHealthColor;
                half4 _TrackColor;
                half4 _BarrierColor;
                float _HealthInnerRadius;
                float _HealthOuterRadius;
                float _BarrierInnerRadius;
                float _BarrierOuterRadius;
                float _LowHealthThreshold;
                float _BlinkFrequency;
                float _BlinkMinAlpha;
                float _HealthFill;
                float _BarrierFill;
                float _BarrierVisible;
            CBUFFER_END

            // 弧の角度方向のにじみ幅の上限。弧の右端（atan2の不連続点）で幅が跳ねるのを抑える
            #define MAX_ANGLE_AA 0.01

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
                output.uv = input.uv;
                return output;
            }

            // 半径 r が [inner, outer] に入っているかを、画面上1px程度のにじみ付きで返す
            float RingMask(float r, float inner, float outer, float aa)
            {
                return smoothstep(inner - aa, inner + aa, r) * (1.0 - smoothstep(outer - aa, outer + aa, r));
            }

            // 弧上の位置 t（0〜1）が fill 以下なら1。fill が0なら何も満たさない
            float FillMask(float t, float fill, float aa)
            {
                return (1.0 - smoothstep(fill - aa, fill + aa, t)) * step(1e-4, fill);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // UV中心を原点にした -1〜1 の座標
                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);

                // 下半分だけ残す（上端の直線を1pxでぼかす）
                float halfMask = saturate(0.5 - p.y / max(fwidth(p.y), 1e-5));

                // 弧上の位置。左端(-1,0)=0、手前中央(0,-1)=0.5、右端(1,0)=1
                float t = atan2(-p.y, -p.x) / PI;
                // 上端の直線ぎわ（ぼかしで半分残る画素）では右端側が -1 付近に折り返すため、1 の先へ戻す。
                // 左端側は 0 付近の負値になるだけなので、しきい値 -0.5 で両者を分ける
                t = t < -0.5 ? t + 2.0 : t;
                float aaR = fwidth(r);
                float aaT = min(fwidth(t), MAX_ANGLE_AA);

                // HPの弧。低HP時は色を切り替えて点滅させる
                float isLow = step(_HealthFill, _LowHealthThreshold) * step(1e-4, _HealthFill);
                float blink = lerp(_BlinkMinAlpha, 1.0, 0.5 + 0.5 * cos(_Time.y * TWO_PI * _BlinkFrequency));
                half4 filledColor = _HealthColor;
                if (isLow > 0.5)
                {
                    filledColor = _LowHealthColor;
                    filledColor.a *= blink;
                }

                half4 health = lerp(_TrackColor, filledColor, FillMask(t, _HealthFill, aaT));
                health.a *= RingMask(r, _HealthInnerRadius, _HealthOuterRadius, aaR);

                // バリアの弧。未取得なら丸ごと消す
                half4 barrier = lerp(_TrackColor, _BarrierColor, FillMask(t, _BarrierFill, aaT));
                barrier.a *= RingMask(r, _BarrierInnerRadius, _BarrierOuterRadius, aaR) * _BarrierVisible;

                // 2本の弧は重ならないので、アルファで重み付けして1色にまとめる
                half alpha = saturate(health.a + barrier.a);
                half3 rgb = (health.rgb * health.a + barrier.rgb * barrier.a) / max(alpha, 1e-4);
                return half4(rgb, alpha * halfMask);
            }
            ENDHLSL
        }
    }
}
