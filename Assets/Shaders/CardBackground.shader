Shader "FGR/CardBackground"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _HoloColorA ("Holo Color A", Color) = (0.3, 0.9, 1.0, 1)
        _HoloColorB ("Holo Color B", Color) = (1.0, 0.4, 0.9, 1)
        _HoloIntensity ("Holo Intensity", Range(0, 2)) = 0.25
        _HoloSpeed ("Holo Speed", Range(-5, 5)) = 0.4
        _HoloScale ("Holo Scale", Range(1, 80)) = 24
        _BandPower ("Band Power", Range(0.5, 8)) = 3

        _PulsePeriod ("Pulse Period", Range(0.5, 10)) = 4
        _ActiveTime ("Active Time", Range(0.05, 1)) = 0.45
        _FadeTime ("Fade Time", Range(0.01, 0.5)) = 0.12

        _HoloNoiseTex ("Holo Noise Texture", 2D) = "white" {}
        _HoloNoiseScale ("Holo Noise Scale", Float) = 3
        _HoloNoiseSpeed ("Holo Noise Speed", Float) = 0.25

        _HoloColor0 ("Holo Color 0", Color) = (0.2, 0.8, 1.0, 1)
        _HoloColor1 ("Holo Color 1", Color) = (0.7, 0.4, 1.0, 1)
        _HoloColor2 ("Holo Color 2", Color) = (1.0, 0.4, 0.9, 1)
        _HoloColor3 ("Holo Color 3", Color) = (1.0, 0.9, 0.3, 1)

        _HoloBandWidth ("Holo Band Width", Range(0.01, 0.5)) = 0.12
        _HoloBandSoftness ("Holo Band Softness", Range(0.001, 0.2)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;

                float4 _HoloColorA;
                float4 _HoloColorB;
                float _HoloIntensity;
                float _HoloSpeed;
                float _HoloScale;
                float _BandPower;

                float _PulsePeriod;
                float _ActiveTime;
                float _FadeTime;

                float4 _HoloNoiseTex_ST;
                float _HoloNoiseScale;
                float _HoloNoiseSpeed;

                float4 _HoloColor0;
                float4 _HoloColor1;
                float4 _HoloColor2;
                float4 _HoloColor3;

                float _HoloBandWidth;
                float _HoloBandSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_HoloNoiseTex);
            SAMPLER(sampler_HoloNoiseTex);

            Varyings vert (Attributes input)
            {
                Varyings output;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = tex.rgb * _BaseColor.rgb;

                float2 uv = input.uv;

                float2 noiseUV = uv * _HoloNoiseScale;
                noiseUV += float2(_Time.y * _HoloNoiseSpeed, _Time.y * _HoloNoiseSpeed * 0.37);

                float noise = SAMPLE_TEXTURE2D(_HoloNoiseTex, sampler_HoloNoiseTex, noiseUV).r;

                float layerCount = 4.0;
                float layer = floor(saturate(noise) * layerCount);
                layer = min(layer, layerCount - 1.0);

                half3 layerColor = _HoloColor0.rgb;

                if (layer < 0.5)
                {
                    layerColor = _HoloColor0.rgb;
                }
                else if (layer < 1.5)
                {
                    layerColor = _HoloColor1.rgb;
                }
                else if (layer < 2.5)
                {
                    layerColor = _HoloColor2.rgb;
                }
                else
                {
                    layerColor = _HoloColor3.rgb;
                }

                float contour = frac(noise * 8.0);

                float band = 1.0 - smoothstep(_HoloBandWidth, _HoloBandWidth + _HoloBandSoftness, contour);
                band += smoothstep(1.0 - _HoloBandWidth - _HoloBandSoftness, 1.0 - _HoloBandWidth, contour);
                band = saturate(band);


                float cycle = frac(_Time.y / _PulsePeriod);
                float fadeIn = smoothstep(0.0, _FadeTime, cycle);
                float fadeOut = 1.0 - smoothstep(_ActiveTime - _FadeTime, _ActiveTime, cycle);
                float pulse = saturate(fadeIn * fadeOut);

                float holoMask = band *  pulse;

                half3 finalColor = baseColor + layerColor * holoMask * _HoloIntensity;

                return half4(finalColor, tex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
