Shader "ChaosDraftParty/ShrinkingIsland/BouncePadRing"
{
    Properties
    {
        [HDR] _BaseColor    ("Base Color", Color)              = (1, 1, 1, 1)
        [HDR] _RimColor     ("Rim Color", Color)               = (1.0, 0.85, 0.35, 1)
        _InnerRadius        ("Inner Radius", Range(0, 1))      = 0.35
        _OuterRadius        ("Outer Radius", Range(0, 1))      = 0.50
        _RingWidth          ("Ring Width", Range(0, 0.5))      = 0.08
        _Progress           ("Progress 0-1", Range(0, 1))      = 0
        _EmissionMul        ("Emission Multiplier", Float)     = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BouncePadRing"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half  _InnerRadius;
                half  _OuterRadius;
                half  _RingWidth;
                half  _Progress;
                half  _EmissionMul;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Quad UV (0..1) を center (0.5, 0.5) からの距離に変換
                float2 centered = input.uv - 0.5;
                float dist = length(centered);

                // Progress (0->1) で内→外へリングを拡張
                half currentRadius = lerp(_InnerRadius, _OuterRadius, _Progress);

                // Ring mask: 内側エッジで立ち上がり、外側エッジで減衰
                half halfWidth = max(_RingWidth, 0.0001h);
                half innerEdge = smoothstep(currentRadius - halfWidth, currentRadius, dist);
                half outerEdge = 1.0h - smoothstep(currentRadius, currentRadius + halfWidth, dist);
                half ringMask  = saturate(innerEdge * outerEdge);

                // 色: Base から Rim (gold) へ progress で遷移
                half3 color = lerp(_BaseColor.rgb, _RimColor.rgb, _Progress);

                // Bloom 連動 emission (HDR > 1 を許容)
                half3 emission = color * _EmissionMul;

                // Alpha フェード: 末尾で消失
                half baseAlpha = lerp(_BaseColor.a, _RimColor.a, _Progress);
                half alpha = (1.0h - _Progress) * ringMask * baseAlpha;

                return half4(emission * ringMask, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
