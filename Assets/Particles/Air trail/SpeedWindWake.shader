Shader "Jordan/VFX/SpeedWindWake"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Range(0,12)) = 4.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "WindWake"
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _FlowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 tint : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 tint : COLOR;
                float2 uv : TEXCOORD0;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.tint = input.tint * _BaseColor;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float t = input.uv.x;
                float width = abs(input.uv.y * 2.0 - 1.0);
                half feather = 1.0h - smoothstep(0.12h, 1.0h, width);
                half core = 1.0h - smoothstep(0.03h, 0.30h, width);
                half envelope = smoothstep(0.0, 0.10, t) * (1.0 - smoothstep(0.52, 1.0, t));
                float phase = frac(t * 1.7 - _Time.y * _FlowSpeed);
                half flow = smoothstep(0.05, 0.25, phase) * (1.0 - smoothstep(0.50, 0.92, phase));
                half opacity = envelope * (feather * 0.24h + core * (0.30h + 0.42h * flow)) * input.tint.a;
                return half4(input.tint.rgb * opacity, opacity);
            }
            ENDHLSL
        }
    }
}
