Shader "FGR/Board"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.1, 0.2, 0.2, 0.1)
        _HighlightColor ("Highlight Color", Color) = (1, 0.85, 0.2, 0.5)

        _GridX ("Grid X", Float) = 5
        _GridY ("Grid Y", Float) = 5
        _LineWidth ("Line Width", Float) = 0.015
        _HighlightLineWidth ("Highlight Line Width", Float) = 0.09

        _BoardOriginSize ("Board Origin Size", Vector) = (0, 0, 1, 1)

        _HighlightTex ("Highlight Texture", 2D) = "red" {}
    }
    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            }
        LOD 100

        Pass
        {
            Name "BoardOverlay"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldVertex : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _HighlightColor;
                float4 _BoardOriginSize; // x = min world x, y = min world z, z = width, w = height
                float _GridX;
                float _GridY;
                float _LineWidth;
                float _HighlightLineWidth;
            CBUFFER_END

            TEXTURE2D(_HighlightTex);
            SAMPLER(sampler_HighlightTex);

            v2f vert (Attributes v)
            {
                v2f o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = posInputs.positionCS;
                o.worldVertex = posInputs.positionWS;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 worldXZ = i.worldVertex.xz;

                float2 boardMin = _BoardOriginSize.xy;
                float2 boardSize = max(_BoardOriginSize.zw, 0.0001);

                float2 boardUV = (worldXZ - boardMin) / boardSize;

                // Outside board = fully transparent.
                if (boardUV.x < 0 || boardUV.x > 1 || boardUV.y < 0 || boardUV.y > 1)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 gridCount = max(float2(_GridX, _GridY), 1.0);
                float2 gridUV = boardUV * gridCount;
                float2 cellFrac = frac(gridUV);
                float2 cellIndex = floor(gridUV);

                float edgeX = min(cellFrac.x, 1.0 - cellFrac.x);
                float edgeY = min(cellFrac.y, 1.0 - cellFrac.y);
                float edgeDistance = min(edgeX, edgeY);

                float gridLine = 1.0 - smoothstep(_LineWidth, _LineWidth * 1.4, edgeDistance);

                float2 maskUV = (cellIndex + 0.5) / gridCount;
                float highlightMask = SAMPLE_TEXTURE2D(_HighlightTex, sampler_HighlightTex, maskUV).r;

                // Highlight only near cell border, center remains transparent.
                float highlightLine = 1.0 - smoothstep(_HighlightLineWidth, _HighlightLineWidth * 1.4, edgeDistance);
                highlightLine *= highlightMask;

                float3 color = _GridColor.rgb;
                float alpha = gridLine * _GridColor.a;

                color = lerp(color, _HighlightColor.rgb, highlightLine);
                alpha = max(alpha, highlightLine * _HighlightColor.a);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
