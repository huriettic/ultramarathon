Shader "Custom/TriangleTexArray"
{
    Properties
    {
        _MainTex("Tex Array", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _ColorArray[50];

            struct Triangle
            {
                float3 v0, v1, v2;
                float4 uv0, uv1, uv2;
            };

            StructuredBuffer<Triangle> outputTriangleBuffer;

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float index : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(uint id : SV_VertexID)
            {
                uint triangleIndex = id / 3;
                uint triangleVertex = id % 3;

                Triangle tri = outputTriangleBuffer[triangleIndex];

                float3 vertexTriangle;
                float4 uvTriangle;

                if (triangleVertex == 0)
                {
                    vertexTriangle = tri.v0;
                    uvTriangle = tri.uv0;
                }
                else if (triangleVertex == 1)
                {
                    vertexTriangle = tri.v1;
                    uvTriangle = tri.uv1;
                }
                else
                {
                    vertexTriangle = tri.v2;
                    uvTriangle = tri.uv2; 
                }

                v2f o;
                o.pos = UnityObjectToClipPos(float4(vertexTriangle, 1.0));
                o.uv = uvTriangle.xy;
                o.index = uvTriangle.z; 
                o.color = _ColorArray[uvTriangle.w];
                o.worldPos = mul(unity_ObjectToWorld, float4(vertexTriangle, 1.0)).xyz;
                return o; 
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                float distance = length(i.worldPos - _WorldSpaceCameraPos);
                float brightnessFactor = lerp(0.3f, 0, clamp(distance / 10.0f, 0, 1));
                float4 brightness = float4(clamp(i.color.rgb + brightnessFactor, 0, 1), i.color.a);
                float4 Col = brightness * UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(i.uv, i.index));
                return Col;
            }
            ENDCG
        }
    }
}