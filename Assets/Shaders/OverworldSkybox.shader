Shader "MineCraft/OverworldSkybox"
{
    Properties
    {
        _SkyTop ("Sky Top", Color) = (0.47, 0.65, 1, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.72, 0.85, 1, 1)
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun Color", Color) = (1, 1, 1, 1)
        _SunSize ("Sun Size", Float) = 0.018
        _MoonDirection ("Moon Direction", Vector) = (0, -1, 0, 0)
        _MoonColor ("Moon Color", Color) = (0.9, 0.9, 0.95, 1)
        _MoonSize ("Moon Size", Float) = 0.014
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0
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
                half4 _SkyTop;
                half4 _SkyHorizon;
                half4 _SunDirection;
                half4 _SunColor;
                half4 _MoonDirection;
                half4 _MoonColor;
                half _SunSize;
                half _MoonSize;
                half _StarBrightness;
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

            half4 SampleCelestial(half3 dir, half3 bodyDir, half bodySize, half4 bodyColor)
            {
                half cosAngle = dot(dir, normalize(bodyDir));
                half edge = 1.0h - bodySize;
                half disc = smoothstep(edge, 1.0h, cosAngle);
                return half4(bodyColor.rgb, bodyColor.a * disc);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 dir = normalize(input.direction);
                half t = saturate(dir.y * 0.5h + 0.5h);
                half3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, t);

                half4 sun = SampleCelestial(dir, _SunDirection.xyz, _SunSize, _SunColor);
                half4 moon = SampleCelestial(dir, _MoonDirection.xyz, _MoonSize, _MoonColor);
                sky = lerp(sky, sun.rgb, sun.a);

                if (_StarBrightness > 0.001h && dir.y > 0.02h)
                {
                    float2 uv = dir.xz / (dir.y + 0.15);
                    float2 cell = floor(uv * 120.0);
                    half star = step(0.992h, Hash21(cell));
                    half twinkle = 0.65h + 0.35h * sin(cell.x * 12.7 + cell.y * 4.1);
                    sky += star * _StarBrightness * twinkle * dir.y;
                }

                sky = lerp(sky, moon.rgb, moon.a);
                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
