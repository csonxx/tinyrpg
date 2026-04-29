Shader "TinyRPG/Accessibility/ColorblindColorMatrix"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _ColorMatrix ("Color Matrix (row-major 3x3)", Vector) = (1,0,0,0, 0,1,0,0, 0,0,1,0)
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float3x3 _ColorMatrix;

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

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Apply color matrix transformation
                // Matrix is in row-major order: row0 = (r,g,b), row1 = (r,g,b), row2 = (r,g,b)
                float3 result;
                result.r = dot(float3(col.r, col.g, col.b), float3(_ColorMatrix[0].r, _ColorMatrix[0].g, _ColorMatrix[0].b));
                result.g = dot(float3(col.r, col.g, col.b), float3(_ColorMatrix[1].r, _ColorMatrix[1].g, _ColorMatrix[1].b));
                result.b = dot(float3(col.r, col.g, col.b), float3(_ColorMatrix[2].r, _ColorMatrix[2].g, _ColorMatrix[2].b));

                return fixed4(result, col.a);
            }
            ENDCG
        }
    }
}
