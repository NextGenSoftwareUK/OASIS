Shader "Hidden/OASIS/PortalGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,1)) = 0.65
        _Intensity ("Intensity", Range(0,3)) = 0.8
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Threshold;
            float _Intensity;
            fixed4 _GlowColor;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float luminance = max(col.r, max(col.g, col.b));
                float glowFactor = saturate((luminance - _Threshold) * 4.0) * _Intensity;
                fixed3 glow = _GlowColor.rgb * glowFactor;
                return fixed4(col.rgb + glow, col.a);
            }
            ENDCG
        }
    }
}




