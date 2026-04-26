// =============================================================================
// Custom/DepthClipLit
// =============================================================================
// 【このシェーダーの目的】
//   DepthClipperController.cs をアタッチした「基準オブジェクト」より
//   カメラに近い位置に描画されるフラグメント（ピクセル）を非表示にする。
//
// 【使い方】
//   1. 基準にしたいオブジェクトに DepthClipperController.cs をアタッチ
//   2. 非表示にしたいオブジェクトのマテリアルのシェーダーをこれに変更
//   3. 実行すると「基準オブジェクトよりカメラ側」が自動で消える
//
// 【深度（Depth）の基礎知識】
//   「深度」= カメラからの距離。値が小さい＝カメラに近い＝前面。
//   このシェーダーでは「リニアアイ深度」を使う。
//   リニアアイ深度 = カメラ中心からの実際の距離（メートル単位）。
//   C# 側での取得: Camera.WorldToViewportPoint(pos).z
// =============================================================================

Shader "Custom/DepthClipLit"
{
    // =========================================================================
    // Properties ブロック
    // =========================================================================
    // Unity Inspector に表示されるパラメータ。
    // 標準の URP Lit シェーダーと同じ項目を揃えている。
    Properties
    {
        _BaseMap("Albedo (RGB)", 2D) = "white" {}   // テクスチャ
        _BaseColor("Color", Color) = (1,1,1,1)      // 乗算カラー
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0) // 自己発光色（HDR対応）
        _Smoothness("Smoothness", Range(0,1)) = 0.5 // 滑らかさ（光の鋭さ）
        _Metallic("Metallic", Range(0,1)) = 0.0     // 金属度
    }

    SubShader
    {
        // =====================================================================
        // SubShader Tags
        // =====================================================================
        // RenderType  : "Opaque" = 不透明オブジェクトとして扱う
        // RenderPipeline: URP 専用であることを明示
        // Queue       : "Geometry" = 通常の不透明オブジェクトと同じ描画順
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // =====================================================================
        // Pass 1: ForwardLit（メインの描画パス）
        // =====================================================================
        // ライティング計算とデプスクリップを行うメインパス。
        // 画面に色として見えるのはこのパスだけ。
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            // Cull Back  = オブジェクトの裏面ポリゴンは描かない（通常の設定）
            // ZWrite On  = 深度バッファに書き込む（他オブジェクトとの前後判定に使われる）
            Cull Back
            ZWrite On

            HLSLPROGRAM
            // -----------------------------------------------------------------
            // #pragma: コンパイラへの指示
            // -----------------------------------------------------------------
            // vertex / fragment = それぞれの関数名を指定
            #pragma vertex Vert
            #pragma fragment Frag

            // シャドウ関連のバリアント（影を受け取るために必要）
            // _MAIN_LIGHT_SHADOWS          = メインライトの影
            // _MAIN_LIGHT_SHADOWS_CASCADE  = カスケードシャドウマップ（距離による解像度切替）
            // _MAIN_LIGHT_SHADOWS_SCREEN   = スクリーンスペースシャドウ
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // 追加ライト（ポイントライト等）のバリアント
            // _ADDITIONAL_LIGHTS_VERTEX = 頂点単位で計算（軽量）
            // _ADDITIONAL_LIGHTS        = ピクセル単位で計算（高品質）
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            // フォグ（霧）効果のバリアント
            #pragma multi_compile_fog

            // -----------------------------------------------------------------
            // インクルード: URP の共通関数・定数を取り込む
            // -----------------------------------------------------------------
            // Core.hlsl     = 座標変換・基本的な計算関数
            // Lighting.hlsl = PBRライティング計算関数（UniversalFragmentPBR 等）
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // -----------------------------------------------------------------
            // テクスチャ宣言
            // -----------------------------------------------------------------
            // TEXTURE2D / SAMPLER はプラットフォーム差を吸収するマクロ。
            // DX11/Metal/Vulkan それぞれで適切なコードに展開される。
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // -----------------------------------------------------------------
            // CBUFFER（定数バッファ）
            // -----------------------------------------------------------------
            // マテリアルのパラメータをまとめてGPUに送るための構造。
            // _BaseMap_ST = テクスチャのタイリング(xy)とオフセット(zw)。
            //               TRANSFORM_TEX マクロが内部で使う。
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _Smoothness;
                half   _Metallic;
            CBUFFER_END

            // -----------------------------------------------------------------
            // グローバル変数（C# の Shader.SetGlobalFloat で設定）
            // -----------------------------------------------------------------
            // CBUFFER の外に置くことで「マテリアルごとではなく全体共通」の値になる。
            // DepthClipperController.cs が毎フレーム更新する。
            // 0 のときはクリッピング無効（OnDisable/OnDestroy 時のリセット値）。
            float _ClipperDepth;

            // -----------------------------------------------------------------
            // 頂点入力構造体（CPU→GPU, オブジェクト空間の生データ）
            // -----------------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;  // 頂点座標（オブジェクト空間）
                float3 normalOS   : NORMAL;    // 法線ベクトル（ライティングに使用）
                float2 uv         : TEXCOORD0; // UV座標（テクスチャの貼り方）
                UNITY_VERTEX_INPUT_INSTANCE_ID // GPU インスタンシング用（同じメッシュを大量描画する最適化）
            };

            // -----------------------------------------------------------------
            // 頂点→フラグメント 受け渡し構造体（クリップ空間〜ワールド空間）
            // -----------------------------------------------------------------
            struct Varyings
            {
                // SV_POSITION: 最終的な画面座標（クリップ空間）
                // ★注意: ラスタライズ後（フラグメントシェーダー到達時）は
                //         .xy = スクリーンピクセル座標, .z = NDC深度, .w = 1/eyeDepth
                //         になるため、.w からアイ深度は取り出せない。
                //         → 頂点シェーダーで別途 eyeDepth として渡す必要がある。
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1; // 法線（ワールド空間）
                float3 positionWS : TEXCOORD2; // 座標（ワールド空間、ライティング計算に使用）

                // ★ デプスクリップの核心部分
                // 頂点シェーダー時点の positionCS.w = リニアアイ深度（カメラからのメートル距離）。
                // TEXCOORD3 で渡すことで、フラグメント間で正しく補間される。
                float  eyeDepth   : TEXCOORD3;

                // フォグ計算用（UNITY_FOG_COORDS(4) = TEXCOORD4 として展開）
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO // VRの左右眼対応
            };

            // -----------------------------------------------------------------
            // 頂点シェーダー（各頂点に対して1回実行）
            // -----------------------------------------------------------------
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // GetVertexPositionInputs: オブジェクト空間→クリップ空間・ワールド空間へ変換
                // GetVertexNormalInputs  : 法線をワールド空間へ変換
                VertexPositionInputs posInputs    = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normalInputs.normalWS;

                // TRANSFORM_TEX: UV にタイリング・オフセットを適用
                // （_BaseMap_ST の xy=タイリング, zw=オフセット を使用）
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // ★ リニアアイ深度の取り出し
                // ラスタライズ前の positionCS.w は「クリップ空間 W」で、
                // これはカメラからの実際の距離（メートル）と等しい。
                // C# の Camera.WorldToViewportPoint(pos).z と同じ単位・空間。
                output.eyeDepth = posInputs.positionCS.w;

                UNITY_TRANSFER_FOG(output, output.positionCS);
                return output;
            }

            // -----------------------------------------------------------------
            // フラグメントシェーダー（各ピクセルに対して1回実行）
            // -----------------------------------------------------------------
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // =============================================================
                // ★★ デプスクリップ判定（このシェーダーの核心）★★
                // =============================================================
                // 条件1: _ClipperDepth > 0  →  C# 側でクリッパーが有効になっている
                // 条件2: eyeDepth < _ClipperDepth  →  このピクセルは基準点よりカメラに近い
                // 両方満たす場合 discard（このピクセルの描画をキャンセル）する。
                //
                // 例: 基準オブジェクトが5m先にあり _ClipperDepth = 5.0 のとき、
                //     3m先のオブジェクトの eyeDepth = 3.0 < 5.0 → discard（消える）
                //     7m先のオブジェクトの eyeDepth = 7.0 > 5.0 → 描画される
                if (_ClipperDepth > 0.0 && input.eyeDepth < _ClipperDepth)
                    discard;

                // テクスチャサンプリング＋カラー乗算
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // -----------------------------------------------------------------
                // InputData: ライティング計算に必要な「入力情報」をまとめる構造体
                // -----------------------------------------------------------------
                InputData inputData = (InputData)0; // ゼロ初期化
                inputData.positionWS              = input.positionWS;
                inputData.normalWS                = normalize(input.normalWS); // 補間で長さが変わるので正規化
                inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(input.positionWS); // カメラ方向
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);   // スクリーンUV
                inputData.fogCoord                = input.fogFactor; // フォグ計算用

                // -----------------------------------------------------------------
                // SurfaceData: マテリアルの「表面の見た目」をまとめる構造体
                // -----------------------------------------------------------------
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo.rgb;   // 基本色
                surfaceData.alpha      = albedo.a;     // 透明度（このシェーダーでは使わないが必要）
                surfaceData.metallic   = _Metallic;    // 金属度（高いほど反射色がアルベドカラーになる）
                surfaceData.smoothness = _Smoothness;  // 滑らかさ（高いほどハイライトが鋭い）
                surfaceData.normalTS   = half3(0,0,1); // ノーマルマップなし（法線は頂点法線をそのまま使用）
                surfaceData.emission   = _EmissionColor.rgb; // 自己発光
                surfaceData.occlusion  = 1.0;          // アンビエントオクルージョン（1=影響なし）

                // UniversalFragmentPBR: URP の物理ベースレンダリング計算をまとめて実行する関数
                // ライトの色・方向・影・間接光など全部ここで処理される
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // フォグを最終カラーに適用（遠くほど霧の色に近づく）
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // =====================================================================
        // Pass 2: ShadowCaster（影の投影パス）
        // =====================================================================
        // 影を「落とす」ための専用パス。色は描かず深度だけを書き込む。
        // ColorMask 0 = 色バッファへの書き込みを完全に無効化。
        // このパスにはデプスクリップを入れていない。
        // 理由: 見えなくなった部分でも影は投影させたい場合が多いため。
        //       影も消したい場合は ForwardLit と同様の discard 処理を追加すること。
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0  // 色は書かない（深度バッファのみ）
            Cull Back

            HLSLPROGRAM
            // ShadowCasterPass.hlsl が ShadowPassVertex / ShadowPassFragment を定義している
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            // スポットライト・ポイントライトの影対応
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ShadowCasterPass.hlsl の内部でこれらを参照するため、
            // ForwardLit パスと同じ構造の CBUFFER が必要
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _Smoothness;
                half   _Metallic;
            CBUFFER_END

            // _Cutoff: アルファカットアウト用閾値。このシェーダーでは未使用だが、
            // ShadowCasterPass.hlsl 内でこの変数名を参照するため宣言だけ必要。
            half _Cutoff;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // =====================================================================
        // Pass 3: DepthOnly（カメラ深度テクスチャ生成パス）
        // =====================================================================
        // URP がポストエフェクトや SSAO のために「深度テクスチャ」を事前生成する際に使う。
        // ColorMask R = 深度値を赤チャンネルに書き込む（URP の内部仕様）。
        // このパスも色は関係なく、深度情報だけが目的。
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

            half _Cutoff; // ShadowCaster 同様、DepthOnlyPass.hlsl が参照するため宣言

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    // エラー時のフォールバック（このシェーダーがコンパイルできない場合に使われる）
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
