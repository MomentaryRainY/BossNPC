Shader "FGR/RainReflect"
{
    Properties
    {
        _WetMaskTex ("Wet Noise Mask", 2D) = "gray" {}
        _WetMaskScale ("Wet Mask Scale", Float) = 0.006
        _WetMaskOffset ("Wet Mask Offset", Vector) = (0, 0, 0, 0)
        _WetThreshold ("Wet Threshold", Range(0, 1)) = 0.45
        _WetSoftness ("Wet Softness", Range(0.001, 0.5)) = 0.08
        _ReflectionAlpha ("Reflection Alpha", Range(0, 1)) = 0.35
        _RippleDistortion ("Ripple Distortion", Range(0, 0.05)) = 0.01
        _RippleHighlightStrength ("Ripple Highlight Strength", Range(0, 1)) = 0.12
        _RippleHighlightAlpha ("Ripple Highlight Alpha", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue" = "Transparent-50"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WetMaskTex);
            SAMPLER(sampler_WetMaskTex);

            TEXTURE2D(_PlanarReflectionTex);
            SAMPLER(sampler_PlanarReflectionTex);

            TEXTURE2D(_RippleTex);
            SAMPLER(sampler_RippleTex);

            float4x4 _PlanarReflectionVP;
            float4 _RippleMapOriginSize;

            CBUFFER_START(UnityPerMaterial)
                float _WetMaskScale;
                float4 _WetMaskOffset;
                float _WetThreshold;
                float _WetSoftness;
                float _ReflectionAlpha;
                float _RippleDistortion;
                float _RippleTexelSize;
                float _RippleHighlightStrength;
                float _RippleHighlightAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.screenPos = ComputeScreenPos(pos.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 wetUV = input.positionWS.xz * _WetMaskScale + _WetMaskOffset.xy;
                float wetNoise = SAMPLE_TEXTURE2D(_WetMaskTex, sampler_WetMaskTex, wetUV).r;
                float wetMask = smoothstep(_WetThreshold, _WetThreshold + _WetSoftness, wetNoise);

                float4 reflClip = mul(_PlanarReflectionVP, float4(input.positionWS, 1));
                float2 reflUV = reflClip.xy / reflClip.w;
                reflUV = reflUV * 0.5 + 0.5;

                float inside =
                    step(0.0, reflUV.x) * step(reflUV.x, 1.0) *
                    step(0.0, reflUV.y) * step(reflUV.y, 1.0);
                
                float2 rippleUV;
                rippleUV.x = (input.positionWS.x - _RippleMapOriginSize.x) / _RippleMapOriginSize.z;
                rippleUV.y = (input.positionWS.z - _RippleMapOriginSize.y) / _RippleMapOriginSize.w;

                float texel = _RippleTexelSize;

                float hL = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUV + float2(-texel, 0)).r;
                float hR = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUV + float2( texel, 0)).r;
                float hD = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUV + float2(0, -texel)).r;
                float hU = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUV + float2(0,  texel)).r;

                float2 rippleNormal = float2(hR - hL, hU - hD);

                float ripple = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUV).r;
                float rippleHighlight = saturate(ripple);

                reflUV += rippleNormal * _RippleDistortion * wetMask;
                reflUV = saturate(reflUV);

                half3 reflected = SAMPLE_TEXTURE2D(
                    _PlanarReflectionTex,
                    sampler_PlanarReflectionTex,
                    reflUV
                ).rgb;

                half3 finalColor = reflected;
                finalColor += half3(0.75, 0.9, 1.0) * rippleHighlight * _RippleHighlightStrength * wetMask;

                float alpha = wetMask * _ReflectionAlpha * inside;
                alpha = saturate(alpha + rippleHighlight * _RippleHighlightAlpha * wetMask * inside);

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}
