Shader "Custom/VertexLit"
{
  Properties
  {
    _MainTex ("Main Texture", 2D) = "white" {}
    [HDR]_Light ("Light", Color) = (0.9, 0.95, 0.85, 1)
    [HDR]_Base ("Base", Color) = (0.5, 0.5, 0.5, 1)
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }

    Cull Off

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
        float4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        float4 color : COLOR;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;

      // float3 lightDir;

      float4 _Base;
      float4 _Light;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.color = v.color;
        
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        // point light center
        // then invert!
        float t = clamp(dot(i.normal, normalize(i.worldPos)), 0, 1);
        half4 l = lerp(_Base, _Light, t);
        float4 col = tex2D(_MainTex, i.uv) * l;
        return float4(i.color.r * col.r, i.color.g * col.g, i.color.b * col.b, 1);

        // i.color *= lerp(_Base, _Light, t);

        // return i.color;
      }
      ENDCG
    }
  }
}
