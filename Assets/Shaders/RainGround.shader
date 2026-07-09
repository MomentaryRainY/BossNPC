Shader "FGR/RainGroundShader"
{
    Properties
    {
        _MainTex ("Ground Texture", 2D) = "white" {}

        _WetMaskTex ("Wet Noise Mask", 2D) = "gray" {}
        _WetMaskScale ("Wet Mask World Scale", Float) = 0.06
        _WetThreshold ("Wet Threshold", Range(0, 1)) = 0.62
        _WetSoftness ("Wet Softness", Range(0.001, 0.5)) = 0.12
        _WetStrength ("Wet Strength", Range(0, 1)) = 0.65

        _WetDarken ("Wet Darken", Range(0, 1)) = 0.22
        _WetTint ("Wet Tint", Color) = (0.55, 0.68, 0.72, 1)

        _DryLightMin ("Dry Light Min", Range(0, 1)) = 0.55
        _DryLightMax ("Dry Light Max", Range(0, 2)) = 1.05

        _WetSpecColor ("Wet Spec Color", Color) = (1, 0.95, 0.85, 1)
        _WetSpecStrength ("Wet Spec Strength", Range(0, 5)) = 1.4
        _WetSpecPower ("Wet Spec Power", Range(8, 256)) = 96

        _WetFresnelStrength ("Wet Fresnel Strength", Range(0, 2)) = 0.35
        _WetFresnelPower ("Wet Fresnel Power", Range(0.5, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_WetMaskTex);
            SAMPLER(sampler_WetMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;

                float _WetMaskScale;
                float _WetThreshold;
                float _WetSoftness;
                float _WetStrength;

                float _WetDarken;
                float4 _WetTint;

                float _DryLightMin;
                float _DryLightMax;

                float4 _WetSpecColor;
                float _WetSpecStrength;
                float _WetSpecPower;

                float _WetFresnelStrength;
                float _WetFresnelPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normal.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = GetShadowCoord(pos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float2 wetUV = input.positionWS.xz * _WetMaskScale;
                float wetNoise = SAMPLE_TEXTURE2D(_WetMaskTex, sampler_WetMaskTex, wetUV).r;

                float wetMask = smoothstep(
                    _WetThreshold - _WetSoftness,
                    _WetThreshold + _WetSoftness,
                    wetNoise
                );

                wetMask *= _WetStrength;

                Light mainLight = GetMainLight(input.shadowCoord);

                float3 normalWS = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float ndl = saturate(dot(normalWS, lightDir));
                float shadow = mainLight.shadowAttenuation;

                float lightFactor = lerp(_DryLightMin, _DryLightMax, ndl);
                lightFactor *= lerp(0.55, 1.0, shadow);

                half3 color = baseTex.rgb;

                // Wet areas are darker and slightly colder.
                half3 wetColor = color;
                wetColor *= 1.0 - _WetDarken;
                wetColor = lerp(wetColor, wetColor * _WetTint.rgb, 0.35);

                color = lerp(color, wetColor, wetMask);

                // Basic scene lighting.
                color *= lightFactor * mainLight.color.rgb;

                // Fake wet specular.
                float spec = pow(saturate(dot(normalWS, halfDir)), _WetSpecPower);
                color += spec * _WetSpecStrength * wetMask * _WetSpecColor.rgb * mainLight.color.rgb;

                // Grazing-angle shine, gives puddles a broader wet sheen.
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _WetFresnelPower);
                color += fresnel * _WetFresnelStrength * wetMask * _WetSpecColor.rgb;

                return half4(color, baseTex.a);
            }

            ENDHLSL
        }
    }
}