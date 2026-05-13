
/*
This fallback is intended for the Unity Standard shader. 

Assumes the following properties exist:
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0

The following are assumed to optionally exist:
    _MetallicGlossMap ("Albedo (RGB)", 2D) = "white" {}
    _GlossMapScale ("GlossMapScale", Range(0,1)) = 1.0
    _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
*/

Shader "Corgi/Raytracing/URPFallbackRaytrace"
{
    SubShader
    {
        Pass
        {
            Name "RaytraceReflectionPass"

            HLSLPROGRAM

            #pragma raytracing URP_ClosestHit
            #include "../Includes/URP/RaytracedReflectionHitPass_URP.cginc"

            ENDHLSL
        }

        Pass
        {
            Name "RaytraceShadowsPass"

            HLSLPROGRAM

            #pragma raytracing URP_ClosestHit_Shadows
            #include "../Includes/URP/RaytracedShadowsHitPass_URP.cginc"

            ENDHLSL
        }

        Pass
        {
            Name "RaytraceExamplePass"

            HLSLPROGRAM

            #pragma raytracing MyHitShader

            // URP shader features
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog 

            // Corgi Raytracing defines 
            #pragma multi_compile _ _CORGI_FOG

            #include "../Includes/RaytraceDataHelpers.cginc"

            struct ExamplePayload
            {
                float4 color;
            };

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;

            [shader("closesthit")]
            void MyHitShader(inout ExamplePayload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
            {

                Vertex v0, v1, v2;
                GetVertexData(v0, v1, v2);

                float2 barycentrics = attributes.barycentrics;
                float3 barycentricCoords = float3(1.0 - barycentrics.x - barycentrics.y, barycentrics.x, barycentrics.y);

                float2 texcoord = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texcoord, v1.texcoord, v2.texcoord, barycentricCoords);

                float4 color = _MainTex.SampleLevel(sampler_MainTex, texcoord, 0);
                payload.color = color;
            }

            ENDHLSL
        }
    }

    // for URP Lit shaders.. 
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
