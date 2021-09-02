Shader "Custom/Gem"
{
  Properties
  {
    [HDR]_Color ("Color", Color) = (1,1,1,1)
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
        // float4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        float4 color : COLOR;
      };

      float _Colored;
      float4 _Color;
      sampler2D _Ramp;

      float rand(float2 co)
      {
        return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
      }

      float4 hsv2rgb(float3 c)
      {
        c = float3(c.x, clamp(c.yz, 0.0, 1.0));
        float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return float4(c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y), 1.0);
      }

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        // o.color = v.color;
        o.worldPos.x += sin(_Time.y);
        o.worldPos.z += sin(_Time.x + _Time.y);
        float t = clamp(dot(o.normal, normalize(o.worldPos)), 0, 1);
        float4 white = float4(1,1,1,1);
        _Color = lerp(float4(0.5, 0.5, 0.5, 1), _Color, _Colored);
        o.color = _Color;
        if (t > 0.8)
        {
          o.color = lerp(_Color, white, (t - 0.8) * 10);
        }
        if (t > 0.9)
        {
          o.color = lerp(_Color, white, 1 - ((t - 0.9) * 10));
        }
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        // i.worldPos.x += sin(_Time.y);
        // i.worldPos.z += sin(_Time.x + _Time.y);
        // float4 inverted = float4(1 - _Color.r, 1 - _Color.g, 1 - _Color.b, 1.0);
        // float t = clamp(dot(i.normal, normalize(i.worldPos)), 0, 1);
        // float4 white = float4(1,1,1,1);
        // float r = rand(i.worldPos.xy);
        // if (r < t)
        // {
        //   return inverted;
        // }
        // return lerp(_Color, hsv2rgb(float3(t, 0.1, 1)), t);
        
        // if (t > 0.8)
        // {
        //   return lerp(_Color, white, (t - 0.8) * 20);
        // }
        // return _Color;

        // float value = (i.color.r + i.color.g + i.color.b) / 3;
        // float4 grayscale = float4(1,1,1,1) * value;
        return i.color;
      }
      ENDCG
    }
  }
}
