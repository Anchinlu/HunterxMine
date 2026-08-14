Shader "MineCraft/GrassSideOverlay"
{
    Properties
    {
        _OverlayMap ("Side Overlay Mask", 2D) = "white" {}
        _GrassTint ("Grass Tint", Color) = (1, 1, 1, 1)
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "MineCraftLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD1;
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
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _OverlayMap);
                output.color = MineCraftResolveVertexColor(input.color);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 overlayTex = SAMPLE_TEXTURE2D(_OverlayMap, sampler_OverlayMap, input.uv);
                clip(overlayTex.r - 0.001h);
                half4 color = half4(overlayTex.rgb * _GrassTint.rgb * input.color.rgb, 1);
                color.rgb *= MineCraftResolveSkyLight();
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
