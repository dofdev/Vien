Shader "Custom/Always"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader
  {
    Tags { "Queue"="Overlay" "RenderType"="Transparent" }
    ZWrite Off
    ZTest Always
    Lighting Off
    Fog { Mode Off }
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      Color [_Color]
    }
  }
}
