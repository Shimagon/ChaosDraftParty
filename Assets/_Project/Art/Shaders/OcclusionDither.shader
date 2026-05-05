// Genshin / FF14 / モンハン / Hat in Time defacto: screen-door dither で網戸状に透過。
// URP Lit ベース、ForwardLit pass のみ自前 fragment で Bayer 4x4 dither + clip。
// ShadowCaster / DepthOnly / DepthNormals / Meta は URP Lit を再利用 (UsePass)。
//
// 利点:
// - 元 material の _BaseMap / _BaseColor / _BumpMap / _Metallic / _Smoothness 等が完全保持される
// - Opaque pass のままなので Z 書込・影が壊れない
// - 近接で点描パターンが見えるが、カートゥーン調と相性◎
//
// 制約:
// - _DitherAmount を UnityPerMaterial CBUFFER 外に置いてるため SRP Batcher 脱落
//   (per-renderer 1 instance のみなのでパフォーマンス影響軽微)
// - shadow / depth は dither しない (M1 暫定、M3 で shadow も dither 化検討)

Shader "Custom/OcclusionDither"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
        _SpecColor("Specular", Color) = (0.5, 0.5, 0.5)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [ToggleUI] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleUI] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Bump Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        _ClearCoatMask("Clear Coat Mask", Float) = 0.0
        _ClearCoatSmoothness("Clear Coat Smoothness", Float) = 0.0

        // 本 shader 固有 (CameraOccluderFader.cs から SetFloat で動的制御)
        _DitherAmount("Dither Amount (0=opaque, 1=full screen-door)", Range(0,1)) = 0

        // URP Lit infrastructure
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // URP Lit shader feature 一式 (元 material の keyword を尊重)
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SPECULAR_COLOR
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment OcclusionDitherFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            // _DitherAmount は UnityPerMaterial CBUFFER 外に置く (SRP Batcher 脱落するが、per-renderer 1 instance なので影響軽微)
            float _DitherAmount;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            // Bayer 4x4 dither matrix。screen-space 座標で 0..15/16 のパターンを返す。
            // 16 段階で十分滑らか、人間の目には dot pattern が均一に見える。
            static const float Bayer4x4[16] = {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            float DitherPattern(float2 screenPos)
            {
                uint2 coord = uint2(screenPos) % 4;
                return Bayer4x4[coord.y * 4 + coord.x];
            }

            // Unity 6 / URP 17 の LitPassFragment は void + out param 形式 (Varyings, out half4, [optional out uint])。
            // ラッパー関数も同じシグネチャで書き、dither clip だけ前段で行う。
            void OcclusionDitherFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                // _DitherAmount=0 → (1-0)-pattern = 1-pattern → 全 0 以上 → clip しない (全表示)
                // _DitherAmount=1 → (1-1)-pattern = -pattern → 全 0 以下 → 全 clip (完全消失)
                // _DitherAmount=0.5 → 0.5-pattern → 約半分が 0 以下 → 半分 clip (網戸)
                float dither = DitherPattern(input.positionCS.xy);
                clip((1.0 - _DitherAmount) - dither - 0.0001);

            #ifdef _WRITE_RENDERING_LAYERS
                LitPassFragment(input, outColor, outRenderingLayers);
            #else
                LitPassFragment(input, outColor);
            #endif
            }
            ENDHLSL
        }

        // shadow / depth / meta は URP Lit を再利用 (M1 暫定、M3 で shadow も dither 化検討)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
