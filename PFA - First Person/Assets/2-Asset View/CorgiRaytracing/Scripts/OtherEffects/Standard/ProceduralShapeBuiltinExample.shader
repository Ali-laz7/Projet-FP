Shader "Unlit/ProceduralShapeBuiltinExample"
{
    Properties
    { 
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _MetallicGlossMap("_MetallicGlossMap", 2D) = "white" {}
        _GlossMapScale("GlossMapScale", Range(0,1)) = 1.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "RaytraceRenderPass"

            HLSLPROGRAM
                // this define tells the whole system we're expecting to use procedural data, so it changes the 
                // AttributeData struct to account for it. it's necessary to define in every pass 
                #define RAYTRACEPROCEDURAL 1
                #include "../../../Shaders/Includes/RaytracedRenderHitPass.cginc" 

                // using #include so that we can share the intersection shader with all of our subshaders
                // unfortunately this block will not get pulled into fallback shaders, so we need to define any raytracing effects in our subshader here too!
                #include "ProceduralShapeExample.cginc"
                #pragma raytracing URP_ClosestHit
            ENDHLSL
        }

        Pass
        {
            Name "RaytraceReflectionPass"

            HLSLPROGRAM

                #define RAYTRACEPROCEDURAL 1
                #pragma raytracing Standard_ClosestHit_Generic
                #include "../../../Shaders/Includes/RaytracedReflectionHitPass.cginc"
                #include "ProceduralShapeExample.cginc"
            ENDHLSL
        }

        Pass
        {
            Name "RaytraceShadowsPass"

            HLSLPROGRAM
                #define RAYTRACEPROCEDURAL 1
                #pragma raytracing Standard_ClosestHit_ShadowsGeneric
                #include "../../../Shaders/Includes/RaytracedShadowsHitPass.cginc"
                #include "ProceduralShapeExample.cginc"
            ENDHLSL
        }
    }
}
