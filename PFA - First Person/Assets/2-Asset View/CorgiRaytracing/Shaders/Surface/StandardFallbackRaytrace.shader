
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

Shader "Corgi/Raytracing/StandardFallbackRaytrace"
{
    SubShader
    {
        Pass
        {
            Name "RaytraceReflectionPass"

            HLSLPROGRAM

                #pragma raytracing Standard_ClosestHit_Generic
                #include "../Includes/RaytracedReflectionHitPass.cginc"
            ENDHLSL
        }

        Pass
        {
            Name "RaytraceShadowsPass"

            HLSLPROGRAM
                #pragma raytracing Standard_ClosestHit_ShadowsGeneric
                #include "../Includes/RaytracedShadowsHitPass.cginc"
            ENDHLSL
        }

        Pass
        {
            Name "RaytraceExamplePass"

            HLSLPROGRAM

            #pragma raytracing MyHitShader

            // Standard shader features 
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _SPECGLOSSMAP
            #pragma shader_feature_local _DETAIL_MULX2
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature_local _PARALLAXMAP

            #pragma multi_compile_fog

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

    // so non-rtx shadows can still work 
    FallBack "Diffuse"
}
