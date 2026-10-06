Shader "ChaosDraftParty/ShrinkingIslandSurface"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Color ("Color", Color) = (1, 1, 1, 1)
        _TopBoost ("Top Light Boost", Range(0, 0.5)) = 0.16
        _SideShade ("Side Shade", Range(0, 0.7)) = 0.28
        _BottomShade ("Bottom Shade", Range(0, 0.7)) = 0.18
        _EdgeDarken ("Edge Darken", Range(0, 0.5)) = 0.16
        _NoiseStrength ("Surface Color Variation", Range(0, 0.2)) = 0.055
        _NoiseScale ("Surface Variation Scale", Range(0.1, 20)) = 4.5
        _RimLight ("Soft Rim Light", Range(0, 0.5)) = 0.1
        _Smoothness ("Specular Smoothness", Range(0, 1)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
                half4 _Color;
                half _TopBoost;
                half _SideShade;
                half _BottomShade;
                half _EdgeDarken;
                half _NoiseStrength;
                half _NoiseScale;
                half _RimLight;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normalize(normal.normalWS);
                output.shadowCoord = GetShadowCoord(pos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half top = saturate(normalWS.y);
                half side = 1.0h - abs(normalWS.y);

                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half wrappedLight = lerp(0.55h, 1.0h, ndotl) * mainLight.shadowAttenuation;

                float noiseA = ValueNoise(input.positionWS.xz * _NoiseScale);
                float noiseB = ValueNoise((input.positionWS.xy + input.positionWS.zy) * (_NoiseScale * 0.45));
                half noise = (half)((noiseA * 0.7 + noiseB * 0.3) * 2.0 - 1.0);

                half3 baseColor = _BaseColor.rgb;
                half3 color = baseColor;
                half noiseMask = lerp(0.22h, 1.0h, top);
                color *= 1.0h + noise * _NoiseStrength * noiseMask;
                color *= 1.0h + top * _TopBoost;
                color *= 1.0h - side * _SideShade;
                color *= 1.0h - saturate(-normalWS.y) * _BottomShade;

                half edge = pow(1.0h - saturate(abs(normalWS.y)), 2.0h);
                color *= 1.0h - edge * _EdgeDarken;

                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0h - saturate(dot(normalWS, viewDir)), 3.0h) * _RimLight;
                color += rim * baseColor;

                half3 halfDir = normalize(mainLight.direction + viewDir);
                half spec = pow(saturate(dot(normalWS, halfDir)), lerp(12.0h, 96.0h, _Smoothness));
                color += spec * _Smoothness * 0.12h;

                color *= wrappedLight;
                color += baseColor * 0.22h;

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
    }
}
