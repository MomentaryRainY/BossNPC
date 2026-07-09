Shader "FGR/SnowStamp"
{
    Properties
    {
        _PrevTex ("Previous", 2D) = "white" {}
        _StampStrength ("Stamp Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PrevTex);
            SAMPLER(sampler_PrevTex);


            float4 _StampUVRadius;
            float _StampStrength;

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float current = SAMPLE_TEXTURE2D(_PrevTex, sampler_PrevTex, input.uv).r;

                float2 stampCenter = _StampUVRadius.xy;
                float2 radius = _StampUVRadius.zw;

                float2 brushUV = (input.uv - stampCenter) / radius * 0.5 + 0.5;

                float inside =
                    step(0.0, brushUV.x) * step(brushUV.x, 1.0) *
                    step(0.0, brushUV.y) * step(brushUV.y, 1.0);

                float dist = length(brushUV - 0.5);
                float brush = 1.0 - smoothstep(0.08, 0.45, dist);
                brush *= brush;
                brush *= inside;
                float stamped = current - brush * _StampStrength;
                float result = saturate(min(current, stamped));

                return half4(result, result, result, 1);
            }

            ENDHLSL
        }
    }
}