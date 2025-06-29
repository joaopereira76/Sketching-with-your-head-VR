﻿Shader "Custom/UniColorPicker/CurcleShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pos = float2(i.uv.x-0.5, i.uv.y-0.5) * 2;
                float len = length(pos);
                if (len > 0.15 || len < 0.12)
                {
                    discard;
                }
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
