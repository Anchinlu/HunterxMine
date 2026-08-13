Shader "MineCraft/GrassSideOverlay"
{
    Properties
    {
        _OverlayMap ("Side Overlay Mask", 2D) = "white" {}
        _GrassTint ("Grass Tint", Color) = (0.57, 0.74, 0.35, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Cull Back
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_OverlayMap);
            SAMPLER(sampler_OverlayMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _OverlayMap_ST;
                half4 _GrassTint;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _OverlayMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Same tint formula as BlockUnlit top: grayscale texture × biome color (tintindex 0).
                half4 overlayTex = SAMPLE_TEXTURE2D(_OverlayMap, sampler_OverlayMap, input.uv);
                clip(overlayTex.r - 0.001h);
                return half4(overlayTex.rgb * _GrassTint.rgb, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
