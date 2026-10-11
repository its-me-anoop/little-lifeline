Shader "IdleClinic/FacadeGlass"
{
    Properties { _Color ("Glass tint", Color) = (0.66,0.85,0.89,0.20) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 position : SV_POSITION; };
            v2f vert(float4 vertex : POSITION)
            {
                v2f output;
                output.position=UnityObjectToClipPos(vertex);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
