// Studio gradient skybox for CharacterLab — soft top/equator/ground fade
// Research: https://timcoster.com/2019/09/03/unity-shadergraph-skybox-quick-tutorial/
Shader "ChaosDraftParty/CharLabSkybox"
{
    Properties
    {
        [HDR] _TopColor       ("Top Color", Color)       = (0.22, 0.26, 0.34, 1)
        [HDR] _EquatorColor   ("Equator Color", Color)   = (0.46, 0.48, 0.54, 1)
        [HDR] _GroundColor    ("Ground Color", Color)    = (0.16, 0.18, 0.22, 1)
        _TopExponent          ("Top Exponent",  Range(0.1, 8)) = 1.6
        _GroundExponent       ("Ground Exponent", Range(0.1, 8)) = 2.2
        _Intensity            ("Intensity", Range(0, 3))  = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _EquatorColor;
                half4 _GroundColor;
                half _TopExponent;
                half _GroundExponent;
                half _Intensity;
            CBUFFER_END

            struct Attr { float4 positionOS : POSITION; };
            struct Vary { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            Vary vert(Attr IN)
            {
                Vary OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Vary IN) : SV_Target
            {
                float3 dir = normalize(IN.positionOS);
                half up   = saturate( dir.y);
                half down = saturate(-dir.y);
                half upW   = pow(up,   _TopExponent);
                half downW = pow(down, _GroundExponent);
                half3 col  = lerp(_EquatorColor.rgb, _TopColor.rgb, upW);
                col        = lerp(col, _GroundColor.rgb, downW);
                return half4(col * _Intensity, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
