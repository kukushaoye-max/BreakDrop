Shader "BreakDrop/SquareCornerArtwork"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                // Correct only the baked corner arcs inside the original image rectangle.
                float2 size=_MainTex_TexelSize.zw;
                float2 p=i.uv*size;
                float radius=40.0*size.x/904.0;
                float dy=min(p.y,size.y-p.y);
                if(dy<radius)
                {
                    float inset=radius-sqrt(max(0.0,radius*radius-(radius-dy)*(radius-dy)))+2.0;
                    p.x=clamp(p.x,inset,size.x-inset);
                }
                return tex2D(_MainTex,p/size)*i.color;
            }
            ENDCG
        }
    }
}

