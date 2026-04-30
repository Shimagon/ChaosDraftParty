// Custom URP glass shader for EGGYBOT clear capsule
// - Fresnel-based alpha (edge opaque, center see-through)
// - Soft diffuse + sharp specular rim
// - Transparent with premultiplied alpha
//
// Research sources:
//   https://80.lv/articles/this-cool-unity-shader-can-be-glass-frost-or-resin
//   https://github.com/omid3098/Unity-URP-GlassShader
//   https://docs.unity3d.com/Manual/StandardShaderFresnel.html
Shader "ChaosDraftParty/EGGYBOTGlass"
{
    Properties
    {
        [HDR] _TintColor        ("Tint Color", Color)      = (0.82, 0.86, 0.92, 1.0)
        _BaseAlpha              ("Base Alpha",     Range(0, 1))  = 0.08
        _EdgeAlpha              ("Edge Alpha",     Range(0, 1))  = 0.92
        _FresnelPower           ("Fresnel Power",  Range(0.5, 8)) = 2.1
        [HDR] _RimColor         ("Rim Color",      Color)        = (1.0, 1.0, 1.0, 1.0)
        _RimPower               ("Rim Power",      Range(1, 32)) = 8.0
        _RimIntensity           ("Rim Intensity",  Range(0, 4))  = 1.2
        _DiffuseTint            ("Diffuse Tint",   Range(0, 1))  = 0.35
        _Smoothness             ("Specular Smoothness", Range(0,1)) = 0.82
        _RefractionStrength     ("Refraction Strength", Range(0, 0.1)) = 0.015
        _RefractionMix          ("Refraction Mix",      Range(0, 1))   = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Transparent"
            "Queue"            = "Transparent"
            "RenderPipeline"   = "UniversalPipeline"
            "IgnoreProjector"  = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off // 両面描画化: hemisphere の裏側 rim 帯も shading されて連続シルエットに
            // 設計意図: Cull Off + ZWrite Off で hemisphere の表裏が同フラグメントに重なる時、
            // 描画順は draw call 順依存（深度ソートなし）。premultiplied alpha (Blend One OneMinusSrcAlpha) なので
            // 二重加算で grazing 帯がやや明るく出る場合がある。_EdgeAlpha / _RimIntensity を上げる際は
            // 二重加算による白飛びを目視確認すること。Z-fighting は ZWrite Off により回避済み。
            // NOTE: Pass 2 (Silhouette) で連続シルエットが描けるなら、この Cull Off は厳密には不要。
            // 裏面 rim の視覚効果が user 確認で不要と判断されたら Cull Back に戻して描画負荷を削減すること。

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _TintColor;
                half   _BaseAlpha;
                half   _EdgeAlpha;
                half   _FresnelPower;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                half   _DiffuseTint;
                half   _Smoothness;
                half   _RefractionStrength;
                half   _RefractionMix;
            CBUFFER_END

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = posIn.positionCS;
                OUT.positionWS = posIn.positionWS;
                OUT.normalWS   = nrmIn.normalWS;
                OUT.screenPos  = ComputeScreenPos(posIn.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 注意: 裏面ポリゴン (Cull Off) では N が反転するため、後段の NdotL/NdotH は
                // saturate により 0 寄りになる。これにより裏面の diffuse / specular は意図的に薄く出る。
                // 一方、fresnel / rim は abs(dot(N, V)) で対称化してあるので両面で連続して見える。
                // → 「裏面は rim のみ表現、diffuse/specular は意図的に抑制」が本 shader の設計。
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);

                // Fresnel — edges face away from camera
                // abs(dot) で grazing 帯の符号クランプを除去 → rim/fresnel が両面で対称になり線が切れない
                half NdotV   = abs(dot(N, V));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Alpha: thin in center, opaque at edges (matches real glass)
                half alpha = lerp(_BaseAlpha, _EdgeAlpha, fresnel);

                // Main directional light — soft half-lambert contribution
                Light mainLight = GetMainLight();
                half3 L = normalize(mainLight.direction);
                half  NdotL = saturate(dot(N, L) * 0.5 + 0.5);
                half3 diffuse = _TintColor.rgb * NdotL * _DiffuseTint;

                // Blinn-Phong specular for that glass highlight
                float3 H = normalize(L + V);
                half NdotH = saturate(dot(N, H));
                half specExp = lerp(4.0, 256.0, _Smoothness);
                half spec = pow(NdotH, specExp) * mainLight.distanceAttenuation;
                half3 specular = mainLight.color * spec;

                // Rim — bright edge glow when viewing grazing angle
                half rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;
                half3 rimColor = _RimColor.rgb * rim;

                // Refraction — sample scene color behind the glass with normal-based distortion
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 distortion = N.xy * _RefractionStrength;
                half3 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture,
                    sampler_CameraOpaqueTexture, screenUV + distortion).rgb;

                half3 rgb = diffuse + specular + rimColor;
                // Blend scene refraction into the base tint (only in the "see-through" center)
                rgb = lerp(rgb, sceneColor * _TintColor.rgb + rgb, _RefractionMix * (1.0 - fresnel));

                // Premultiplied alpha output (matches Blend One OneMinusSrcAlpha)
                return half4(rgb * alpha, alpha);
            }
            ENDHLSL
        }

        // Pass for the ShadowCaster — we skip so transparent glass doesn't cast hard shadows
        // (URP auto-handles missing ShadowCaster pass by falling back to not casting)
    }

    Fallback Off
}
