Shader "FGR/SnowGradientShader"
{
    Properties
    {
        _SnowTex ("Snow Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _SnowNormalTex ("Snow Normal Texture", 2D) = "white" {}
        _SnowHeightTex ("Snow Height Texture", 2D) = "white" {}
        _SnowMetallicTex ("Snow Metallic Texture", 2D) = "white" {}
        _GroundTex ("Ground Texture", 2D) = "white" {}
        _SnowMaskScale ("Snow Mask Scale", range(0.015, 0.04)) = 0.04
        _SnowHeightBlend ("Snow Height Blend", range(0.1, 0.25)) = 0.5
        _SnowThreshold ("Snow Threshold", range(0.45, 0.65)) = 0.5
        _SnowSoftness ("Snow Softness", range(0.05, 0.15)) = 0.5
        _SlopeThreshold ("slope Threshold", range(0.2, 0.5)) = 0.5
    }
    SubShader
    {
        Tags 
        { "RenderType"="Opaque"
          "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SnowTex_ST;
                float4 _GroundTex_ST;
                float _SnowMaskScale;
                float _SnowHeightBlend;
                float _SnowThreshold;
                float _SnowSoftness;
                float _SlopeThreshold;
            CBUFFER_END

            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);

            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_SnowNormalTex);
            SAMPLER(sampler_SnowNormalTex);

            TEXTURE2D(_SnowHeightTex);
            SAMPLER(sampler_SnowHeightTex);

            TEXTURE2D(_SnowMetallicTex);
            SAMPLER(sampler_SnowMetallicTex);

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            v2f vert (appdata input)
            {
                v2f output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normal.normalWS;
                output.tangentWS = float4(normal.tangentWS, input.tangentOS.w);
                output.uv = input.uv;

                return output;
            }

            half4 frag (v2f input) : SV_Target
            {
                float2 groundUV = TRANSFORM_TEX(input.uv, _GroundTex);
                float2 snowUV = TRANSFORM_TEX(input.uv, _SnowTex);
                float2 maskUV = input.positionWS.xz * _SnowMaskScale;

                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, maskUV).r;
                float snowHeight = SAMPLE_TEXTURE2D(_SnowHeightTex, sampler_SnowHeightTex, snowUV).r;

                float maskValue = noise + (snowHeight - 0.5) * _SnowHeightBlend;

                float snowMask = smoothstep(
                    _SnowThreshold - _SnowSoftness,
                    _SnowThreshold + _SnowSoftness,
                    maskValue
                );

                float slopeMask = smoothstep(_SlopeThreshold, 1.0, saturate(input.normalWS.y));
                snowMask *= slopeMask;

                half3 groundColor = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, groundUV).rgb;
                half3 snowColor = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, snowUV).rgb;

                half3 baseColor = lerp(groundColor, snowColor, snowMask);

                float3 groundNormalTS = float3(0, 0, 1);

                float3 snowNormalTS = UnpackNormal(
                    SAMPLE_TEXTURE2D(_SnowNormalTex, sampler_SnowNormalTex, snowUV)
                );

                float3 normalTS = normalize(lerp(groundNormalTS, snowNormalTS, snowMask));

                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);

                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);
                normalWS = normalize(mul(normalTS, tbn));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float ndl = saturate(dot(normalWS, lightDir));
                float shadow = mainLight.shadowAttenuation;

                float snowSmoothness = SAMPLE_TEXTURE2D(_SnowMetallicTex, sampler_SnowMetallicTex, snowUV).a;
                float smoothness = lerp(0.25, snowSmoothness, snowMask);

                float specPower = lerp(16.0, 128.0, smoothness);
                float spec = pow(saturate(dot(normalWS, halfDir)), specPower);

                half3 diffuse = baseColor * ndl * mainLight.color.rgb * shadow;
                half3 ambient = baseColor * 0.25;

                half3 specColor = lerp(half3(0.04, 0.04, 0.04), half3(0.8, 0.9, 1.0), snowMask);
                half3 specular = spec * specColor * smoothness * mainLight.color.rgb * shadow;

                half3 finalColor = ambient + diffuse + specular;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
