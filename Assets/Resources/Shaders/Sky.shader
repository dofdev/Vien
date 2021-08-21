Shader "Custom/Sky"
{
  Properties
  {
    
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }

    // Cull Back
    Blend SrcAlpha OneMinusSrcAlpha

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
      };

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        // point light center
        // then invert!
        // UnityWorldSpaceViewDir
        float t = 1 - clamp(dot(i.normal / 2, UnityWorldSpaceViewDir(i.worldPos)), 0, 1);
        return fixed4(0.3, 0.3, 0.3, clamp(t - 0.2, 0, 1));

        // i.color *= lerp(_Base, _Light, t);

        // return i.color;
      }
      ENDCG
    }
  }
}
