Shader "MineCraft/OverworldSkybox"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.47, 0.65, 1, 1)
        _FogHorizonColor ("Fog Horizon Color", Color) = (0.75, 0.85, 1, 1)
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0
        _StarAngle ("Star Angle Rad", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _FogHorizonColor;
                half _StarBrightness;
                half _StarAngle;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.direction = normalize(positionWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 RotateXZ(float2 xz, half angle)
            {
                half s = sin(angle);
                half c = cos(angle);
                return float2(c * xz.x - s * xz.y, s * xz.x + c * xz.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 dir = normalize(input.direction);

                // MC ref: flat sky disc color; horizon tint from fog (AtmosphericFogEnvironment sky mix).
                half elevation = saturate(dir.y);
                half horizonMix = 1.0h - pow(saturate(elevation * 1.35h), 0.25h);
                half3 sky = lerp(_SkyColor.rgb, _FogHorizonColor.rgb, horizonMix * 0.85h);

                if (_StarBrightness > 0.001h && dir.y > 0.08h)
                {
                    float2 starDir = RotateXZ(dir.xz / (dir.y + 0.22), _StarAngle);
                    // MC ref: ~1500 stars — sparse grid + high hash threshold.
                    float2 cell = floor(starDir * 88.0);
                    half star = step(0.997h, Hash21(cell));
                    half twinkle = 0.65h + 0.35h * sin(cell.x * 12.7 + cell.y * 4.1);
                    half elevationFade = dir.y * dir.y;
                    sky += star * _StarBrightness * twinkle * elevationFade * 0.85h;
                }

                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
