Shader "FGR/IndreactGrassShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 2)) = 0.25
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2
        _WindScale ("Wind Scale", Range(0.01, 2)) = 0.25
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.3, 0)
        _WindNoiseTex ("Wind Noise Texture", 2D) = "gray" {}
        _WindNoiseScale ("Wind Noise Scale", Range(0.01, 2)) = 0.15
        _WindNoiseSpeed ("Wind Noise Speed", Range(0, 5)) = 0.3
        _WindTurbulence ("Wind Turbulence", Range(0, 2)) = 0.5
        _GroundTintTex ("Ground Tint Texture", 2D) = "white" {}
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.5
        _TintMapOriginSize ("Tint Map Origin Size", Vector) = (0, 0, 100, 100)
        _BendSpecColor ("Bend Spec Color", Color) = (1, 1, 0.75, 1)
        _BendSpecStrength ("Bend Spec Strength", Range(0, 3)) = 0.8
        _BendSpecPower ("Bend Spec Power", Range(4, 64)) = 24

        _GrassShadowColor ("Grass Shadow Color", Color) = (0.03, 0.05, 0.025, 1)
        _GrassShadowAlpha ("Grass Shadow Alpha", Range(0, 1)) = 0.18
        _GrassShadowYOffset ("Grass Shadow Y Offset", Float) = 0.018
        _GrassShadowFlatten ("Grass Shadow Flatten", Range(0.2, 3)) = 1.4
        _GrassShadowDirection ("Grass Shadow Direction", Vector) = (-0.5, 0.3, 0, 0)
        _GrassShadowLength ("Grass Shadow Length", Range(0, 2)) = 0.25
        _GrassShadowWidth ("Grass Shadow Width", Range(0, 1)) = 0.08
        _GrassShadowWindStrength ("Grass Shadow Wind Strength", Range(0, 0.3)) = 0.05
    }
    SubShader
    {
        Tags 
        { 
          "RenderType" = "Transparent"
          "Queue" = "Transparent-20"
          "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        
        Pass
        {
            Name "FakeGrassShadow"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float4x4> _Matrices;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            float _WindSpeed;
            float _WindScale;
            float4 _GrassShadowColor;
            float _GrassShadowAlpha;
            float _GrassShadowYOffset;
            float _GrassShadowFlatten;
            float4 _GrassShadowDirection;
            float _GrassShadowLength;
            float _GrassShadowWidth;
            float _GrassShadowWindStrength;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float height01 : TEXCOORD1;
            };
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                float4x4 m = _Matrices[input.instanceID];

                float3 localPos = input.vertex.xyz;
                float3 originWS = mul(m, float4(0, 0, 0, 1)).xyz;
                float3 worldPos = mul(m, float4(localPos, 1)).xyz;

                float height01 = saturate(localPos.y * 2.0);

                // Lower the grass to near ground
                worldPos.y = originWS.y + _GrassShadowYOffset;

                //worldPos.xz = lerp(originWS.xz, worldPos.xz, _GrassShadowFlatten);
                worldPos.xz = originWS.xz;
                float2 shadowDir = normalize(_GrassShadowDirection.xz + 0.0001);
                worldPos.xz += shadowDir * height01 * _GrassShadowLength;

                float side = input.uv.x * 2.0 - 1.0;
                float2 sideDir = float2(-shadowDir.y, shadowDir.x);
                worldPos.xz += sideDir * side * _GrassShadowWidth;

                float wave = sin(_Time.y * _WindSpeed + dot(originWS.xz, shadowDir) * _WindScale);
                worldPos.xz += shadowDir * wave * _GrassShadowWindStrength;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.height01 = height01;

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half4 tex = tex2D(_MainTex, input.uv);
                clip(tex.a - _Cutoff);

                float alpha = tex.a * _GrassShadowAlpha;

                alpha *= lerp(1.0, 0.45, input.height01);

                return half4(_GrassShadowColor.rgb, alpha);
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
            #pragma target 4.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            StructuredBuffer<float4x4> _Matrices;

            float4 _BaseColor;
            float4 _PlayerPosRadius;
            float _BendStrength;
            float _WindStrength;
            float _WindSpeed;
            float _WindScale;
            float4 _WindDirection;

            sampler2D _GroundTintTex;
            float4 _GroundTintTex_ST;
            float _TintStrength;
            float4 _TintMapOriginSize;

            sampler2D _WindNoiseTex;
            float _WindNoiseScale;
            float _WindNoiseSpeed;
            float _WindTurbulence;

            float4 _BendSpecColor;
            float _BendSpecStrength;
            float _BendSpecPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD2;
                float bendAmount : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                float4x4 m = _Matrices[v.instanceID];

                float3 localPos = v.vertex.xyz;

                float3 worldPos = mul(m, float4(localPos, 1)).xyz;
                float3 originalWorldPos = worldPos;

                
                 // wind
                float3 instanceOrigin = mul(m, float4(0, 0, 0, 1)).xyz;
                float instanceRand = Hash12(instanceOrigin.xz);

                float2 baseDir = normalize(_WindDirection.xz + 0.0001);
                float2 sideDir = float2(-baseDir.y, baseDir.x);

                float heightWeight = saturate(localPos.y * 2.0);
                heightWeight *= heightWeight;

                // bigger part
                float2 macroUV = originalWorldPos.xz * (_WindNoiseScale * 5.0);
                macroUV += baseDir * _Time.y * _WindNoiseSpeed;

                float macro = tex2Dlod(_WindNoiseTex, float4(macroUV, 0, 0)).r;
                float macroAmount = macro * 2.0 - 1.0;

                // smaller part
                float2 microUV = originalWorldPos.xz * (_WindNoiseScale * 0.5);
                microUV += float2(0.37, 0.81) * _Time.y * (_WindNoiseSpeed * 1.2);
                microUV += instanceRand * 4.0;

                float micro = tex2Dlod(_WindNoiseTex, float4(microUV, 0, 0)).r;
                float microAmount = micro * 2.0 - 1.0;

                float phase = instanceRand * 6.28318;
                float wave = sin(_Time.y * _WindSpeed + dot(originalWorldPos.xz, baseDir) * _WindScale + phase);

                // weighted combine
                
                float windAmount = wave * 0.75 + macroAmount * 0.3 + microAmount * 0.08;

                // add noised direction
                float2 noiseDir = normalize(baseDir + sideDir * microAmount * _WindTurbulence);

                float2 windOffset = noiseDir * windAmount * _WindStrength * heightWeight;
                worldPos.xz += windOffset;

                float windBendAmount = saturate(abs(windAmount) * heightWeight);

                // player interaction
                float3 playerPos = _PlayerPosRadius.xyz;
                float radius = _PlayerPosRadius.w;

                float dist = distance(originalWorldPos.xz, playerPos.xz);
                float bend = saturate(1.0 - dist / radius);

                float3 away = normalize(float3(worldPos.x - playerPos.x, 0, worldPos.z - playerPos.z) + 0.0001);
                float3 playerOffset = away * bend * _BendStrength * heightWeight;

                worldPos += playerOffset;
                
                float playerBendAmount = bend * heightWeight;

                float3 normalWS = normalize(mul((float3x3)m, v.normal));
                float3 bendDir = normalize(float3(worldPos.x - originalWorldPos.x, 0, worldPos.z - originalWorldPos.z) + 0.0001);

                float bendAmount = saturate(playerBendAmount * 0.05 + windBendAmount * 0.7);
                normalWS = normalize(lerp(normalWS, float3(-bendDir.x, 0.6, -bendDir.z), bendAmount));

                v2f o;
                o.vertex = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.shadowCoord = TransformWorldToShadowCoord(worldPos);
                o.worldPos = worldPos;
                o.normalWS = normalWS;
                o.bendAmount = bendAmount;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);

                float2 tintUV;
                tintUV.x = (i.worldPos.x - _TintMapOriginSize.x) / _TintMapOriginSize.z / 3;
                tintUV.y = (i.worldPos.z - _TintMapOriginSize.y) / _TintMapOriginSize.w / 3;

                half4 groundTint = tex2D(_GroundTintTex, tintUV);

                col.rgb = lerp(col.rgb, groundTint.rgb, _TintStrength);

                Light mainLight = GetMainLight(i.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float shadow = mainLight.shadowAttenuation;
                float ndl = saturate(dot(normalize(i.normalWS), normalize(mainLight.direction)));

                float directLight = lerp(0.75, 1.05, ndl);
                float shadowLight = lerp(0.75, 1.1, shadow);

                col.rgb *= directLight * shadowLight;

                float3 normalWS = normalize(i.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(lightDir + viewDir);

                float spec = pow(saturate(dot(normalWS, halfDir)), _BendSpecPower);

                col.rgb += spec * i.bendAmount * _BendSpecColor.rgb * _BendSpecStrength;
                
                float heightWeight = saturate(i.vertex.y * 2.0);
                float rootDark = 1.0 - heightWeight;
                col.rgb *= lerp(1.0, 0.65, rootDark * 0.4);
                return col;
            }
            ENDHLSL
        }
    }
}
