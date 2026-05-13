Shader "Unlit/ProceduralShapeShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        
        // SRP batching compatibility for Clear Coat (Not used in Lit)
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader 
    {
        // note: removed the normal vert/frag rendering shader since we plan on only using this for procedural geometry (raytracing intersection) shaders
        // maybe in the future it would be good to have a non-rtx fallback for intersection shaders..? 

        Pass
        {
            Name "RaytraceRenderPass"

            HLSLPROGRAM
                // this define tells the whole system we're expecting to use procedural data, so it changes the 
                // AttributeData struct to account for it. it's necessary to define in every pass 
                #define RAYTRACEPROCEDURAL 1
                #include "../../../Shaders/Includes/URP/RaytracedRenderHitPass_URP.cginc"

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
                #pragma raytracing URP_ClosestHit
                #include "../../../Shaders/Includes/URP/RaytracedReflectionHitPass_URP.cginc"
                #include "ProceduralShapeExample.cginc"
            ENDHLSL
        }
        
        Pass
        {
            Name "RaytraceShadowsPass"
        
            HLSLPROGRAM
                #define RAYTRACEPROCEDURAL 1
                #pragma raytracing URP_ClosestHit_Shadows
                #include "../../../Shaders/Includes/URP/RaytracedShadowsHitPass_URP.cginc"
                #include "ProceduralShapeExample.cginc"
            ENDHLSL
        }
    }

    FallBack "Corgi/Raytracing/URPFallbackRaytrace"    
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
