Shader "Custom/VertexLit"
{
  Properties
  {
    _MainTex ("Main Texture", 2D) = "white" {}
    _Light ("Light", Color) = (0.9, 0.95, 0.85, 1)
    _Base ("Base", Color) = (0.5, 0.5, 0.5, 1)
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
      #include "UnityStandardUtils.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        fixed4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal : NORMAL;
        fixed4 color : COLOR;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;

      // float3 lightDir;

      fixed4 _Base;
      fixed4 _Light;

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

      fixed4 frag (v2f i) : SV_Target
      {
        // point light center
        // then invert!
        float t = clamp(dot(i.normal, normalize(i.worldPos)), 0, 1);
        half4 l = lerp(_Base, _Light, t);
        return tex2D(_MainTex, i.uv) * l;

        // i.color *= lerp(_Base, _Light, t);

        // return i.color;
      }
      ENDCG
    }
  }
}
