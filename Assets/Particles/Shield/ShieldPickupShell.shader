Shader "Jordan/VFX/ShieldPickupShell"
{
    Properties
    {
        _BaseColor ("Particle Tint", Color) = (1,1,1,1)
        _CellCount ("Cells Around Equator", Range(6,32)) = 18
        _CellOpacity ("Cell Edge Opacity", Range(0,1)) = 0.24
        _RimPower ("Rim Falloff", Range(0.5,6)) = 2.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "ShieldShell"
            Cull Back
            ZWrite Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _CellCount;
                half _CellOpacity;
                half _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }

            float HexEdgeDistance(float2 p)
            {
                const float2 repeat = float2(1.0, 1.7320508);
                float2 a = (frac(p / repeat) - 0.5) * repeat;
                float2 b = (frac(p / repeat + 0.5) - 0.5) * repeat;
                float2 nearest = abs(dot(a, a) < dot(b, b) ? a : b);
                return 0.5 - max(nearest.x, dot(nearest, float2(0.5, 0.8660254)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                half rim = pow(1.0h - facing, _RimPower);
                float2 gridUV = input.uv * float2(1.0, 0.5) * round(_CellCount);
                float edge = HexEdgeDistance(gridUV);
                float pixelWidth = max(fwidth(edge), 0.001);
                half gridLine = 1.0 - smoothstep(0.018 - pixelWidth, 0.018 + pixelWidth, edge);
                half latitude = min(input.uv.y, 1.0 - input.uv.y);
                half polarFade = smoothstep(0.035h, 0.18h, latitude);
                half pulse = 0.78h + 0.22h * sin(input.uv.y * 13.0h - _Time.y * 3.0h);
                half cells = gridLine * polarFade * pulse * _CellOpacity;
                half opacity = saturate(0.035h + 0.82h * rim + cells) * input.color.a;
                half3 glow = input.color.rgb * (1.0h + 0.5h * rim);
                return half4(glow * opacity, opacity);
            }
            ENDHLSL
        }
    }
}
