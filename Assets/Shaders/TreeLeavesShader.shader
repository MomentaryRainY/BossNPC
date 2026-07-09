Shader "FGR/TreeLeavesShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Leaf Tint", Color) = (1, 1, 1, 1)
        _ColorDepth ("Color Depth", Range(0.4, 1.4)) = 1

        _ShadowColor ("Shadow Color", Color) = (0.35, 0.5, 0.25, 1)
        _ShadeThreshold ("Shade Threshold", Range(0, 1)) = 0.5
        _ShadeSoftness ("Shade Softness", Range(0, 0.5)) = 0.05
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.7
        _ShakingFrequency ("Shaking Frequency", Float) = 2
        _ShakingStrength ("Shaking Strength", Float) = 0.05

        _EdgeColor ("Edge Color", Color) = (0.05, 0.12, 0.03, 1)
        _EdgeStrength ("Edge Strength", Range(0, 1)) = 0.6
        _EdgeWidth ("Edge Width", Range(1, 4)) = 1

        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        _CameraFadeStart ("Camera Fade Start", Float) = 5
        _CameraFadeEnd ("Camera Fade End", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Cutoff;
                float _ShakingFrequency;
                float _ShakingStrength;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;
                float shake = sin(_Time.y * _ShakingFrequency) * _ShakingStrength;
                float heightWeight = saturate(posOS.y);
                posOS.x += shake * heightWeight;

                output.positionCS = TransformObjectToHClip(posOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(tex.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Tags{"LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float _ColorDepth;
                float _ShadeThreshold;
                float _ShadowStrength;
                float _Cutoff;
                float _ShakingFrequency;
                float _ShakingStrength;
                float4 _EdgeColor;
                float _EdgeStrength;
                float _EdgeWidth;
                float4 _MainTex_TexelSize;
                float _CameraFadeEnd;
                float _CameraFadeStart;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 posOS = input.positionOS.xyz;
                float shake = sin(_Time.y * _ShakingFrequency) * _ShakingStrength;
                float heightWeight = saturate(posOS.y);
                posOS.x += shake * heightWeight;
                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                output.positionCS = pos.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = pos.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // alpha Cutoff
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(tex.a - _Cutoff);

                // view depth cutoff
                float cameraDistance = distance(input.positionWS, _WorldSpaceCameraPos);
                float fade = smoothstep(_CameraFadeEnd, _CameraFadeStart, cameraDistance);

                float ghost = saturate(fade);

                // remain partial pixels
                float2 screenUV = input.positionCS.xy / input.positionCS.w;
                float noise = frac(sin(dot(screenUV * 173.13, float2(12.9898, 78.233))) * 43758.5453);

                // clip more if close
                clip(ghost - noise * 0.8);

                half luminance = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half3 baseColor = _Color.rgb * luminance * _ColorDepth;

                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS);

                // simple fake normal
                normalWS = normalize(lerp(normalWS, half3(0, 1, 0), 0.45));

                // edge
                float edge = 1.0 - smoothstep(_Cutoff, _Cutoff + 0.15 * _EdgeWidth, tex.a);
                edge = saturate(edge);

                // light
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half shade = step(_ShadeThreshold, ndl);

                half3 lightColor = baseColor * (mainLight.color.rgb * 1.75);
                half3 darkColor = baseColor * 0.75;

                half3 toonColor = lerp(darkColor, lightColor, shade);
                half3 finalColor = lerp(baseColor, toonColor, _ShadowStrength);

                finalColor *= 1.15;
                finalColor = lerp(finalColor, _EdgeColor.rgb, edge * _EdgeStrength);

                return half4(finalColor, tex.a * _Color.a);
            }
            ENDHLSL
        }
    }
}