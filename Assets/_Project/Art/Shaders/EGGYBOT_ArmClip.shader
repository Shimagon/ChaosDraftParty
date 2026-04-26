// EGGYBOT arm shader: SoftPlastic + SDF-based clipping (revolutionary).
// Stencil approach は URP forward renderer で信頼性に欠ける + screen-space で角度依存。
// 代わりに 3D SDF（球体距離）で「体の中にあるピクセル」を確実に discard。
//
// 仕組み:
//   1. Body の world 中心 / 半径 / 上端 Y を C# (EggybotShaderBinder.cs) が material に毎フレーム配信
//   2. fragment shader で current pixel の worldPos を取得
//   3. worldPos.y < bodyTopY AND length(worldPos - bodyCenter) < bodyRadius なら discard
//   4. 結果: 体の半球内にある手のピクセルは確実に消える、外は見える
//
// メリット:
//   - 描画順非依存（stencil は body→arm の順序が必須）
//   - カメラ角度非依存（stencil は screen-space で誤動作する）
//   - URP / HDRP 共通で動く
Shader "ChaosDraftParty/EGGYBOTArmClip"
{
    Properties
    {
        _BaseColor              ("Base Color", Color) = (0.96, 0.55, 0.65, 1.0)
        _AmbientTint            ("Ambient Tint", Color) = (0.75, 0.60, 0.68, 1.0)
        _SSSColor               ("SSS Color (back-light)", Color) = (1.0, 0.55, 0.55, 1.0)
        _SSSDistortion          ("SSS Distortion", Range(0, 1)) = 0.25
        _SSSPower               ("SSS Power", Range(1, 8)) = 3.8
        _SSSScale               ("SSS Scale", Range(0, 3)) = 1.1
        _WrapFactor             ("Wrap Lighting", Range(0, 1)) = 0.55
        [HDR] _RimColor         ("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower               ("Rim Power", Range(1, 12)) = 4.0
        _RimIntensity           ("Rim Intensity", Range(0, 2)) = 0.8
        _Smoothness             ("Specular Smoothness", Range(0, 1)) = 0.68
        _SpecularIntensity      ("Specular Intensity", Range(0, 2)) = 0.85
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        _InteriorShadow         ("Interior Shadow (backface darken)", Range(0, 1)) = 0.22

        // SDF clip parameters (set by EggybotShaderBinder.cs at runtime)
        _BodyCenter             ("Body Center (world)", Vector) = (0, 0.5, 0, 0)
        _BodyRadius             ("Body Radius", Float) = 0.336
        _BodyTopY               ("Body Top Y (world)", Float) = 0.5
        _ClipMargin             ("Clip Margin (mm)", Range(-50, 50)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _AmbientTint;
                half4 _SSSColor;
                half  _SSSDistortion;
                half  _SSSPower;
                half  _SSSScale;
                half  _WrapFactor;
                half4 _RimColor;
                half  _RimPower;
                half  _RimIntensity;
                half  _Smoothness;
                half  _SpecularIntensity;
                half  _Cull;
                half  _InteriorShadow;
                float4 _BodyCenter;
                float  _BodyRadius;
                float  _BodyTopY;
                float  _ClipMargin;
            CBUFFER_END

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
                float4 shadowCoord: TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS  = posIn.positionCS;
                OUT.positionWS  = posIn.positionWS;
                OUT.normalWS    = nrmIn.normalWS;
                OUT.shadowCoord = GetShadowCoord(posIn);
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE isFront : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // === SDF-based clip: discard if pixel is inside body's hemisphere ===
                float marginM = _ClipMargin * 0.001;  // mm → m
                if (IN.positionWS.y < _BodyTopY + marginM)
                {
                    float3 toBody = IN.positionWS - _BodyCenter.xyz;
                    float dist = length(toBody);
                    if (dist < _BodyRadius + marginM)
                    {
                        discard;
                    }
                }

                float3 N = normalize(IN.normalWS);
                bool front = IS_FRONT_VFACE(isFront, true, false);
                if (!front) N = -N;

                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);

                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 L = normalize(mainLight.direction);
                half shadow = mainLight.shadowAttenuation;

                half NdotL    = dot(N, L);
                half wrapDiff = saturate((NdotL + _WrapFactor) / (1.0 + _WrapFactor));
                half3 diffuse = _BaseColor.rgb * wrapDiff * mainLight.color * (0.5 + 0.5 * shadow);

                half3 ambient = _AmbientTint.rgb * _BaseColor.rgb * 0.45;

                float3 sssDir = L + N * _SSSDistortion;
                half  sssDot  = pow(saturate(dot(-V, sssDir)), _SSSPower) * _SSSScale;
                half3 sss     = _SSSColor.rgb * sssDot * mainLight.color * (front ? 1.0 : 0.0);

                half NdotV = saturate(dot(N, V));
                half rim   = pow(1.0 - NdotV, _RimPower) * _RimIntensity * (front ? 1.0 : 0.0);
                half3 rimC = _RimColor.rgb * rim;

                float3 H    = normalize(L + V);
                half NdotH  = saturate(dot(N, H));
                half specEx = lerp(8.0, 256.0, _Smoothness);
                half spec   = pow(NdotH, specEx) * _SpecularIntensity * shadow * (front ? 1.0 : 0.0);
                half3 specC = mainLight.color * spec;

                half3 final = diffuse + ambient + sss + rimC + specC;

                if (!front) final *= (1.0 - _InteriorShadow);

                return half4(final, 1.0);
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
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct AttrShadow { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct VaryShadow { float4 positionCS : SV_POSITION; };

            VaryShadow vertShadow(AttrShadow IN)
            {
                VaryShadow OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _LightDirection));
                #if UNITY_REVERSED_Z
                posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = posCS;
                return OUT;
            }

            half4 fragShadow(VaryShadow IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    Fallback Off
}
