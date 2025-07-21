Shader "Unlit/Pinch"
{
    Properties
    {
        _Strength("Strength", Range(-0.5, 0.5)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off

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
                float4 screenPos : TEXCOORD1;
            };

            float _Strength;
            
            sampler2D _GrabPassTransparent;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 subtractedUV = i.uv - float2(0.5, 0.5);
                float2 multipliedUV = normalize(subtractedUV) * (_Strength * saturate(0.5 - length(subtractedUV)));
                float4 screenPosition = float4(i.screenPos.xy / i.screenPos.w, 0, 0);
                float2 screenAddedUV = screenPosition + multipliedUV;
                float4 result = tex2D(_GrabPassTransparent, screenAddedUV);
                
                return result;
            }
            
            ENDCG
        }
    }
}
