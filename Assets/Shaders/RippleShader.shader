Shader "FGR/RippleMask"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.08
        _Strength ("Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _RingWidth;
                float _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 center = input.uv - 0.5;
                float dist = length(center);

                float ringCore = abs(dist - 0.35);
                float ring = 1.0 - smoothstep(_RingWidth, _RingWidth + 0.08, ringCore);
                ring *= smoothstep(_RingWidth + 0.08, _RingWidth, ringCore);

                float fade = 1.0 - smoothstep(0.35, 0.5, dist);

                float value = ring * fade * _Strength * input.color.a;

                return half4(value, value, value, 1);
            }

            ENDHLSL
        }
    }
}