Shader "Custom/Gem"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }

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
        float3 normal : NORMAL;
        // fixed4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        // fixed4 color : COLOR;
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
        o.uv = v.uv;
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        // o.color = v.color;
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        float t = clamp(dot(i.normal, normalize(i.worldPos)), 0, 1);
        float r = rand(i.worldPos.xy);
        if (r < t)
        {
          return fixed4(1 - _Color.r, 1 - _Color.g, 1 - _Color.b, 1.0);
        }
        return _Color;
      }
      ENDCG
    }
  }
}
