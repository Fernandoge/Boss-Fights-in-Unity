Shader "BossFights/DodgeBubble"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.85, 1, 1)
        _RimPower ("Rim Power", Float) = 2.5
        _Fill ("Fill", Range(0, 1)) = 0.12
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _RimPower;
            float _Fill;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float rim : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.rim = 1 - saturate(dot(normalize(v.normal), viewDir));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = saturate(_Fill + pow(i.rim, _RimPower)) * _Intensity;
                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
