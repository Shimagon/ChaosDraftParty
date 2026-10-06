Shader "ChaosDraftParty/Skybox/ShrinkingIslandGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.18, 0.70, 0.95, 1)
        _HorizonColor ("Horizon Color", Color) = (1.00, 0.78, 0.48, 1)
        _BottomColor ("Bottom Color", Color) = (0.16, 0.10, 0.38, 1)
        _HorizonHeight ("Horizon Height", Range(-1, 1)) = -0.08
        _HorizonSoftness ("Horizon Softness", Range(0.01, 1)) = 0.35
        _Exposure ("Exposure", Range(0, 4)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            float _HorizonHeight;
            float _HorizonSoftness;
            float _Exposure;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.direction = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float y = i.direction.y;
                float topT = smoothstep(_HorizonHeight, _HorizonHeight + _HorizonSoftness, y);
                float bottomT = smoothstep(_HorizonHeight, _HorizonHeight - _HorizonSoftness, y);
                fixed4 upper = lerp(_HorizonColor, _TopColor, topT);
                fixed4 lower = lerp(_HorizonColor, _BottomColor, bottomT);
                fixed4 color = y >= _HorizonHeight ? upper : lower;
                color.rgb *= _Exposure;
                return color;
            }
            ENDCG
        }
    }
}
