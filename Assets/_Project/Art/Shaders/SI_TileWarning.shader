Shader "ChaosDraftParty/ShrinkingIsland/TileWarning"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _WarningProgress ("Warning 0-1", Range(0, 1)) = 0
        _WarningColor ("Warning Color", Color) = (1.0, 0.4, 0.3, 1)
        _WarningTintLerp ("Tint Lerp Max", Range(0, 1)) = 0.4
        _EdgeEmissionMul ("Edge Emission", Float) = 2.5
        // _PulseFrequency: WCAG 2.1 epilepsy guideline で >3Hz はリスク域。AAA defacto 2.0-3.0Hz 帯。
        //                  複数 tile 同時点滅で flash 周波数累積する場合あるため、低めに設定推奨。
        _PulseFrequency ("Pulse Hz", Float) = 2.5
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+5"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #ifndef PI
                #define PI 3.14159265359
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _WarningColor;
                half _WarningProgress;
                half _WarningTintLerp;
                half _EdgeEmissionMul;
                half _PulseFrequency;
                half _EdgeWidth;
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
                half3 normalWS = normalize(input.normalWS);

                // 1. Base color * map
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseRgb = baseSample.rgb * _BaseColor.rgb;

                // 2. Warning tint (full red 禁止、最大 _WarningTintLerp までで lerp)
                half tintAmount = _WarningProgress * _WarningTintLerp;
                half3 tintedRgb = lerp(baseRgb, _WarningColor.rgb, tintAmount);

                // 3. Lit 計算 (Lambert wrapped + main light shadow)
                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half wrappedLight = lerp(0.55h, 1.0h, ndotl) * mainLight.shadowAttenuation;

                half3 litColor = tintedRgb * wrappedLight * mainLight.color;
                litColor += tintedRgb * 0.22h; // ambient fill

                // 4. Edge mask (UV 外周ほど 1)
                float2 centered = abs(input.uv - 0.5) * 2.0; // 0..1
                float edgeDistX = centered.x - (1.0 - _EdgeWidth);
                float edgeDistY = centered.y - (1.0 - _EdgeWidth);
                float edgeDist = max(edgeDistX, edgeDistY);
                half edgeMask = saturate(edgeDist / max(_EdgeWidth, 1e-4));

                // 5. Pulse (0..1)
                half pulse = (half)(sin(_Time.y * _PulseFrequency * 2.0 * PI) * 0.5 + 0.5);

                // 6. Emission (HDR で書き出して Bloom 連動)
                half3 emission = _WarningColor.rgb * _EdgeEmissionMul * edgeMask * pulse * _WarningProgress;

                half3 finalColor = litColor + emission;

                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _WarningColor;
                half _WarningProgress;
                half _WarningTintLerp;
                half _EdgeEmissionMul;
                half _PulseFrequency;
                half _EdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
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
                half4 _WarningColor;
                half _WarningProgress;
                half _WarningTintLerp;
                half _EdgeEmissionMul;
                half _PulseFrequency;
                half _EdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
