Shader "Hidden/Chess CGI/Quest HUD Resolve"
{
    Properties { _MainTex ("Premultiplied HUD", 2D) = "black" {} }
    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half4 frag(v2f_img input) : SV_Target
            {
                half4 color = tex2D(_MainTex, input.uv);
                return half4(color.rgb / max(color.a, 1.0h / 255.0h), 1);
            }
            ENDCG
        }
    }
}
