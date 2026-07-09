Shader "FGR/SnowTessellationShader"
{
    Properties
    {
        _SnowTex ("Snow Texture", 2D) = "white" {}
        _SnowDeformTex ("Snow Deform Tex", 2D) = "white" {}

        _TessellationFactor ("Tessellation Factor", Range(1, 32)) = 4
        _FootprintDepth ("Footprint Depth", Range(0.04, 0.12)) = 0.1
        _SurfaceTexScale ("Surface Texture Scale", Range(0.01, 2)) = 0.15
        _FootprintDarken ("Footprint Darken", Range(0, 1)) = 0.35
        _BottomTex ("Bottom Texture", 2D) = "gray" {}
        _SnowTint ("Snow Tint", Color) = (0.9, 0.9, 0.9, 1)
        _BottomColor ("Bottom Color", Color) = (0.55, 0.58, 0.6, 1)
        _RevealStrength ("Reveal Strength", Range(0, 1)) = 0.8
        _RidgeHeight ("Ridge Height", Range(0.01, 0.04)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);

            TEXTURE2D(_SnowDeformTex);
            SAMPLER(sampler_SnowDeformTex);

            TEXTURE2D(_BottomTex);
            SAMPLER(sampler_BottomTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SnowTex_ST;
                float4x4 _SnowWorldToLocal;
                float4 _SnowMapOriginSize;

                float _TessellationFactor;
                float _FootprintDepth;
                float _SurfaceTexScale;
                float _FootprintDarken;
                float4 _SnowTint;
                float4 _BottomColor;
                float _RevealStrength;
                float _RidgeHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct ControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 surfaceUV : TEXCOORD2;
                float2 deformUV : TEXCOORD3;
            };

            ControlPoint vert(Attributes input)
            {
                ControlPoint output;
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                output.tangentOS = input.tangentOS;
                output.uv = input.uv;
                return output;
            }

            TessellationFactors PatchConstantFunction(InputPatch<ControlPoint, 3> patch)
            {
                TessellationFactors f;

                float tess = max(1.0, _TessellationFactor);

                f.edge[0] = tess;
                f.edge[1] = tess;
                f.edge[2] = tess;
                f.inside = tess;

                return f;
            }

            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("fractional_odd")]
            [patchconstantfunc("PatchConstantFunction")]
            ControlPoint Hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            [domain("tri")]
            Varyings Domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch,
                float3 bary : SV_DomainLocation)
            {
                Varyings output;

                float3 positionOS =
                    patch[0].positionOS.xyz * bary.x +
                    patch[1].positionOS.xyz * bary.y +
                    patch[2].positionOS.xyz * bary.z;

                float3 normalOS =
                    patch[0].normalOS * bary.x +
                    patch[1].normalOS * bary.y +
                    patch[2].normalOS * bary.z;

                float4 tangentOS =
                    patch[0].tangentOS * bary.x +
                    patch[1].tangentOS * bary.y +
                    patch[2].tangentOS * bary.z;

                VertexPositionInputs pos = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normal = GetVertexNormalInputs(normalize(normalOS), tangentOS);

                float3 localPos = mul(_SnowWorldToLocal, float4(pos.positionWS, 1)).xyz;

                float2 deformUV;
                deformUV.x = (localPos.x - _SnowMapOriginSize.x) / _SnowMapOriginSize.z;
                deformUV.y = (localPos.z - _SnowMapOriginSize.y) / _SnowMapOriginSize.w;

                float deform = SAMPLE_TEXTURE2D_LOD(_SnowDeformTex, sampler_SnowDeformTex, deformUV, 0).r;
                float footprint = 1.0 - deform;

                float3 worldPos = pos.positionWS;
                worldPos -= normal.normalWS * footprint * _FootprintDepth;

                float2 texel = 1.0 / 1024.0;

                float hL = SAMPLE_TEXTURE2D_LOD(_SnowDeformTex, sampler_SnowDeformTex, deformUV + float2(-texel.x, 0), 0).r;
                float hR = SAMPLE_TEXTURE2D_LOD(_SnowDeformTex, sampler_SnowDeformTex, deformUV + float2( texel.x, 0), 0).r;
                float hD = SAMPLE_TEXTURE2D_LOD(_SnowDeformTex, sampler_SnowDeformTex, deformUV + float2(0, -texel.y), 0).r;
                float hU = SAMPLE_TEXTURE2D_LOD(_SnowDeformTex, sampler_SnowDeformTex, deformUV + float2(0,  texel.y), 0).r;

                float edge = length(float2(hR - hL, hU - hD));
                float ridge = edge;

                worldPos += normal.normalWS * (ridge * _RidgeHeight - footprint * _FootprintDepth);

                output.positionWS = worldPos;
                output.normalWS = normal.normalWS;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.surfaceUV = localPos.xz * _SurfaceTexScale;
                output.deformUV = deformUV;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float deform = SAMPLE_TEXTURE2D(_SnowDeformTex, sampler_SnowDeformTex, input.deformUV).r;
                float footprint = 1.0 - deform;

                half3 snowColor = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, input.surfaceUV).rgb;
                half3 compactedColor = snowColor * half3(0.72, 0.76, 0.78);

                half3 topColor = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, input.surfaceUV).rgb * _SnowTint.rgb;
                half3 bottomColor = SAMPLE_TEXTURE2D(_BottomTex, sampler_BottomTex, input.surfaceUV).rgb * _BottomColor.rgb;

                half reveal = saturate(footprint * _RevealStrength);

                half3 baseColor = lerp(topColor, bottomColor, reveal);

                baseColor = lerp(
                    baseColor,
                    compactedColor,
                    footprint * _FootprintDarken
                );

                float2 texel = 1.0 / 1024.0;

                float hL = SAMPLE_TEXTURE2D(_SnowDeformTex, sampler_SnowDeformTex, input.deformUV + float2(-texel.x, 0)).r;
                float hR = SAMPLE_TEXTURE2D(_SnowDeformTex, sampler_SnowDeformTex, input.deformUV + float2( texel.x, 0)).r;
                float hD = SAMPLE_TEXTURE2D(_SnowDeformTex, sampler_SnowDeformTex, input.deformUV + float2(0, -texel.y)).r;
                float hU = SAMPLE_TEXTURE2D(_SnowDeformTex, sampler_SnowDeformTex, input.deformUV + float2(0,  texel.y)).r;

                float edge = length(float2(hR - hL, hU - hD));
                baseColor += edge * half3(0.9, 0.96, 1.0);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 normalWS = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float ndl = saturate(dot(normalWS, lightDir));
                float shadow = mainLight.shadowAttenuation;

                float lightFactor = lerp(0.45, 1.05, ndl) * lerp(0.55, 1.0, shadow);

                half3 color = baseColor * lightFactor * mainLight.color.rgb;

                float spec = pow(saturate(dot(normalWS, halfDir)), 48);
                color += spec * 0.12 * mainLight.color.rgb;

                return half4(color, 1);
            }

            ENDHLSL
        }
    }
}