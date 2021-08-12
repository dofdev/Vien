Shader "Custom/Add"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }

    Blend One One
    Cull Off
    ZWrite Off
    ZTest Less

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        fixed4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        fixed4 color : COLOR;
      };

      fixed4 _Color;
      float rand(float2 co)
      {
        return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
      }

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.color = v.color;
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        // float r = rand(i.worldPos.xy);
        // if (r < 0.1 - i.color.r)
        // {
        //   return fixed4(0, 0, 0, 0);
        // }
        return i.color * _Color;
      }
      ENDCG
    }
  }
}