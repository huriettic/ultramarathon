Shader "Custom/TexArray"
{
    Properties
    {
        _MainTex("Tex Array", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass   
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float index : TEXCOORD1;
                float4 color : COLOR;
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _ColorArray[50];

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv.xy;
                o.index = v.uv.z; 
                o.color = _ColorArray[v.uv.w];
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {  
                return UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(i.uv, i.index)) * i.color; 
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
