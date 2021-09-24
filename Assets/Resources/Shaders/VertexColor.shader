Shader "Custom/VertexColor"
{
  Properties
  {
    
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }

    Stencil{
      Ref 1
      Comp Equal
    }

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;        
        float4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float4 color : COLOR;
      };

      float _Colored;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        float value = (v.color.r + v.color.r + v.color.g + v.color.g + v.color.g + v.color.b) / 6;
        float4 grayscale = float4(1,1,1,1) * value;
        o.color = lerp(grayscale, v.color, _Colored);
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        return i.color;
      }
      ENDCG
    }
  }
}
