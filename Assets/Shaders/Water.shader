Shader "MineCraft/Water"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (0.25, 0.45, 0.85, 0.75)
        [IntRange] _FrameCount ("Frame Count", Range(1, 64)) = 32
        _FrameTime ("Frame Time (MC ticks)", Float) = 1
        _TickRate ("Game Ticks Per Second", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _FrameCount;
                float _FrameTime;
                float _TickRate;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = MineCraftResolveVertexColor(input.color);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float frame = floor(fmod(_Time.y * _TickRate / max(_FrameTime, 0.001), _FrameCount));
                float invFrames = 1.0 / max(_FrameCount, 1.0);
                float2 atlasUv = float2(input.uv.x, (input.uv.y * invFrames) + frame * invFrames);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUv);
                half4 color = tex * _BaseColor * input.color;
                color.rgb *= MineCraftResolveSkyLight();
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
