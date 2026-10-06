// Splatoon / Hat in Time / TUNIC defacto: screen-space Sobel outline (depth + normal edge detection)。
// URP 17 / Unity 6 で FullScreenPassRendererFeature 経由で実行。
// ChaosDraftParty cartoon パーティ TPS 用、4 色 EGGYBOT を AAA cartoon 感に引き上げる。

Shader "ChaosDraftParty/StylizedOutline"
{
    Properties
    {
        [HDR] _OutlineColor    ("Outline Color", Color)         = (0.06, 0.04, 0.03, 1)
        _DepthThreshold        ("Depth Threshold (× depth)", Range(0, 10))   = 1.5
        _NormalThreshold       ("Normal Threshold (0..1)",   Range(0, 1))    = 0.4
        _Thickness             ("Thickness (px)",            Range(0.5, 3))  = 1.0
        _FadeNear              ("Fade Near (m)",             Range(0, 50))   = 8
        _FadeFar               ("Fade Far (m)",              Range(1, 200))  = 40
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "StylizedOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _OutlineColor;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _Thickness;
            float  _FadeNear;
            float  _FadeFar;

            // 線形 depth 取得 (eye space、m 単位)
            float LinDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            // Sobel 8-tap kernel:
            //  a b c
            //  d . e
            //  f g h
            // gx = -a +c -2d +2e -f +h
            // gy = -a -2b -c +f +2g +h
            float SobelMag(float a, float b, float c, float d, float e, float f, float g, float h)
            {
                float gx = -a + c - 2.0 * d + 2.0 * e - f + h;
                float gy = -a - 2.0 * b - c + f + 2.0 * g + h;
                return sqrt(gx * gx + gy * gy);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // FIX 1: stereo eye index (VR / single-pass instanced で uv が正しくスケールされる必須 setup)
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.texcoord;
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // skybox / 無限遠は outline スキップ (depth raw=0)
                float rawDepth = SampleSceneDepth(uv);
                if (rawDepth <= 0.0001)
                    return col;

                // FIX 2: _BlitTexture_TexelSize は Render Graph で binding 不安定 → _CameraDepthTexture_TexelSize を使う
                float2 ts = _CameraDepthTexture_TexelSize.xy * _Thickness;

                // === Depth Sobel (8-tap) ===
                float d00 = LinDepth(uv + float2(-1, -1) * ts);
                float d10 = LinDepth(uv + float2( 0, -1) * ts);
                float d20 = LinDepth(uv + float2( 1, -1) * ts);
                float d01 = LinDepth(uv + float2(-1,  0) * ts);
                float d21 = LinDepth(uv + float2( 1,  0) * ts);
                float d02 = LinDepth(uv + float2(-1,  1) * ts);
                float d12 = LinDepth(uv + float2( 0,  1) * ts);
                float d22 = LinDepth(uv + float2( 1,  1) * ts);
                float depthEdge = SobelMag(d00, d10, d20, d01, d21, d02, d12, d22);

                // === Normal Sobel (8-tap、center との角度差) ===
                // FIX 3: URP 17 では SampleSceneNormals は 2 引数 (sampler 必須)
                float3 nc = SampleSceneNormals(uv, sampler_PointClamp);
                #define ND(o) (1.0 - dot(nc, SampleSceneNormals(uv + (o) * ts, sampler_PointClamp)))
                float n00 = ND(float2(-1, -1));
                float n10 = ND(float2( 0, -1));
                float n20 = ND(float2( 1, -1));
                float n01 = ND(float2(-1,  0));
                float n21 = ND(float2( 1,  0));
                float n02 = ND(float2(-1,  1));
                float n12 = ND(float2( 0,  1));
                float n22 = ND(float2( 1,  1));
                float normEdge = SobelMag(n00, n10, n20, n01, n21, n02, n12, n22);

                // depth は中心 depth に比例した threshold (遠いほど許容大、近いほど厳しい)
                float centerD = LinDepth(uv);
                // 真因 #2 fix: centerD 乗算は床/天井の緩い depth 勾配を全部捨てる → 絶対値で比較
                float dEdge = step(_DepthThreshold * 0.01, depthEdge);
                float nEdge = step(_NormalThreshold, normEdge);
                float edge = saturate(max(dEdge, nEdge));

                // 距離 fade: near より近 = 全 outline、far より遠 = 0
                float fade = 1.0 - smoothstep(_FadeNear, _FadeFar, centerD);
                edge *= fade;

                // outline を color に lerp (alpha で強度)
                return lerp(col, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
