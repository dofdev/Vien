Shader "Custom/Oriel"
{
  Properties
  {
    [IntRange] _StencilRef ("Stencil Reference Value", Range(0, 255)) = 0
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }

    Stencil
    {
      Ref [_StencilRef]
      Comp Always
      Pass Replace
    }

    Pass
    {
      ZWrite Off
      Color [_Color]
    }
  }
}
