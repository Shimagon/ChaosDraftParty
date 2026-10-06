Shader "ChaosDraftParty/ShrinkingIsland/TileDissolve"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _DissolveProgress ("Dissolve 0-1", Range(0, 1)) = 0
        _DissolveScale ("Noise Scale", Float) = 8.0
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        _DissolveEdgeColor ("Edge Color", Color) = (1.0, 0.6, 0.2, 1)
        _EdgeEmissionMul ("Edge Emission Mul", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+5"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _DissolveProgress;
                float _DissolveScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                float _EdgeEmissionMul;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth gradient noise (value noise with smoothstep interpolation)
            float GradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // FBM (2 octaves) — adds organic crumble feel
            float FBM(float2 p)
            {
                float n = GradientNoise(p) * 0.65;
                n += GradientNoise(p * 2.13 + 17.0) * 0.35;
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normalize(normal.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(pos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Dissolve noise sampled in world-space XZ so adjacent tiles look continuous
                float2 noiseUV = input.positionWS.xz * _DissolveScale;
                float noise = FBM(noiseUV);

                // threshold > 0  → keep pixel
                // threshold <= 0 → discard
                // _DissolveProgress=0 → noise > 0 always → kept (until noise == 0)
                // To guarantee full keep at 0 and full clip at 1, remap.
                float progress = lerp(-0.01, 1.01, _DissolveProgress);
                float alphaCutoff = noise - progress;

                clip(alphaCutoff);

                // Edge band: pixels close to the cut line glow
                float edgeWidth = max(_DissolveEdgeWidth, 1e-4);
                half edge = 1.0h - saturate(alphaCutoff / edgeWidth);

                // Base color * texture
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseRgb = baseSample.rgb * _BaseColor.rgb;

                // Standard URP lit shading
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half wrappedLight = lerp(0.55h, 1.0h, ndotl) * mainLight.shadowAttenuation;

                half3 litColor = baseRgb * (wrappedLight * mainLight.color);
                litColor += baseRgb * 0.22h; // ambient floor

                // Edge emission (warm orange) — HDR for Bloom pickup
                half3 edgeEmission = _DissolveEdgeColor.rgb * (_EdgeEmissionMul * edge);

                half3 finalColor = litColor + edgeEmission;

                return half4(finalColor, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _DissolveProgress;
                float _DissolveScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                float _EdgeEmissionMul;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float Hash21_SC(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float GradientNoise_SC(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21_SC(i);
                float b = Hash21_SC(i + float2(1, 0));
                float c = Hash21_SC(i + float2(0, 1));
                float d = Hash21_SC(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM_SC(float2 p)
            {
                float n = GradientNoise_SC(p) * 0.65;
                n += GradientNoise_SC(p * 2.13 + 17.0) * 0.35;
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                output.positionWS = positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 noiseUV = input.positionWS.xz * _DissolveScale;
                float noise = FBM_SC(noiseUV);
                float progress = lerp(-0.01, 1.01, _DissolveProgress);
                float alphaCutoff = noise - progress;
                clip(alphaCutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _DissolveProgress;
                float _DissolveScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                float _EdgeEmissionMul;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float Hash21_DO(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float GradientNoise_DO(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21_DO(i);
                float b = Hash21_DO(i + float2(1, 0));
                float c = Hash21_DO(i + float2(0, 1));
                float d = Hash21_DO(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM_DO(float2 p)
            {
                float n = GradientNoise_DO(p) * 0.65;
                n += GradientNoise_DO(p * 2.13 + 17.0) * 0.35;
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 noiseUV = input.positionWS.xz * _DissolveScale;
                float noise = FBM_DO(noiseUV);
                float progress = lerp(-0.01, 1.01, _DissolveProgress);
                float alphaCutoff = noise - progress;
                clip(alphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
