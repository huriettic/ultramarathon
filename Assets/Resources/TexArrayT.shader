Shader "Custom/TexArrayT"
{
    Properties
    {
        _MainTex("Tex Array", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

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
                float3 worldPos : TEXCOORD2;
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
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
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
    FallBack "Diffuse"
}
