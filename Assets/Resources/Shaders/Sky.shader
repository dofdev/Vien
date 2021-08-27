Shader "Custom/Sky"
{
  Properties
  {
    
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }

    Blend One One
    Cull Back
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
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        float4 color : COLOR;
      };

      float _Colored;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.color = float4(1,1,1,1);
        
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        // point light center
        // then invert!
        // UnityWorldSpaceViewDir
        float t = 1 - clamp(dot(i.normal / 2, UnityWorldSpaceViewDir(i.worldPos)), 0, 1);
        i.color = float4(0.05, 0.05, 0.05, 1) * clamp(t - 0.2, 0, 1);

        float value = (i.color.r + i.color.g + i.color.b) / 3;
        float4 grayscale = float4(1,1,1,1) * value;
        return lerp(grayscale, i.color, _Colored);
      }
      ENDCG
    }
  }
}
