Shader "Hidden/NightVisionSimple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (0, 1, 0.3, 1)
        _Brightness ("Brightness", Range(0, 5)) = 2
        _Vignette ("Vignette", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _Tint;
            float _Brightness;
            float _Vignette;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Convert to grayscale
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Apply tint and brightness
                col.rgb = gray * _Tint.rgb * _Brightness;
                
                // Vignette effect
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * _Vignette * 2;
                col.rgb *= vignette;
                
                return col;
            }
            ENDCG
        }
    }
}