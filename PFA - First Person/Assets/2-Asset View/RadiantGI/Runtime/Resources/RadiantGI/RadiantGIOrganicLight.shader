Shader "Hidden/Kronnect/RadiantGIOrganicLight"
{
SubShader
{
    ZWrite Off ZTest Always Blend Off Cull Off

    HLSLINCLUDE
    #pragma target 3.0
    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    #include "UnityCG.cginc"
    #include "RadiantGI_Common.hlsl"
    ENDHLSL

  Pass { // 0
      Name "Radiant GI Organic Light Pass"
      Blend One One
      HLSLPROGRAM
      #pragma vertex VertRGI
      #pragma fragment FragOrganicLight
      #pragma multi_compile_local _ _DISTANCE_BLENDING
      #include "RadiantGIOrganicLightPass.hlsl"
      ENDHLSL
  }

}
}


	