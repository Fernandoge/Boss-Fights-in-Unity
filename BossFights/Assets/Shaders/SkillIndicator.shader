Shader "Indicators/SkillIndicator"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.15, 0.1, 0.22)
        _FillColor ("Fill Color", Color) = (1, 0.25, 0.1, 0.55)
        _EdgeColor ("Edge Color", Color) = (1, 0.55, 0.3, 0.9)
        _Fill ("Fill", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width (world units)", Range(0, 0.5)) = 0.08
        _Size ("Size (width, length)", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _FillColor;
            fixed4 _EdgeColor;
            float _Fill;
            float _EdgeWidth;
            float4 _Size;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = lerp(_BaseColor, _FillColor, step(i.uv.y, _Fill));

                // Bright band at the leading edge of the fill, constant width in world units
                float frontDistance = abs(i.uv.y - _Fill) * _Size.y;
                col.rgb += step(frontDistance, 0.15) * step(0.001, _Fill) * step(_Fill, 0.999) * 0.35;

                // Border, constant thickness in world units regardless of the indicator size
                float2 edgeDistance = min(i.uv, 1.0 - i.uv) * _Size.xy;
                col = lerp(col, _EdgeColor, step(min(edgeDistance.x, edgeDistance.y), _EdgeWidth));
                return col;
            }
            ENDCG
        }
    }
}
