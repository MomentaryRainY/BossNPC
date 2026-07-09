Shader "FGR/GroundShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            Tags{"LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 shadowCoord : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                VertexPositionInputs pos = GetVertexPositionInputs(v.vertex.xyz);

                o.vertex = pos.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.shadowCoord = GetShadowCoord(pos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                Light mainLight = GetMainLight(i.shadowCoord);

                float shadow = mainLight.shadowAttenuation;
                float shadowFactor = lerp(0.45, 1.0, shadow);

                half4 col = tex2D(_MainTex, i.uv);
                return col * shadowFactor;
            }
            ENDHLSL
        }
    }
}
