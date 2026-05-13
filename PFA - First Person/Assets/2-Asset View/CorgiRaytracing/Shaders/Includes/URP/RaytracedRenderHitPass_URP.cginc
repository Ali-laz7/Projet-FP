
#ifndef RAYDATAHELPERSINCLUDED
#include "../../Includes/RaytraceDataHelpers.cginc"
#include "../../Includes/RenderPayload.cginc"
#endif

// Corgi Raytracing defines 
#pragma multi_compile _ _CORGI_FOG

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

Texture2D _BaseMap;
SamplerState sampler_BaseMap;
float4 _BaseMap_ST;

float4 _BaseColor;

#ifdef _METALLICGLOSSMAP
Texture2D _MetallicGlossMap;
SamplerState sampler_MetallicGlossMap;
float4 _MetallicGlossMap_ST; 
#endif

float _Smoothness;
float _Metallic;
float _GlossMapScale; 

#if defined(_ALPHATEST_ON)
float _Cutoff;
#endif

// UnityStandardInput.cginc
half2 MetallicGloss(float2 uv, float4 color)
{
    half2 mg;

#ifdef _METALLICGLOSSMAP
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    float2 uvMetallicGloss = TRANSFORM_TEX(uv, _MetallicGlossMap);
    mg.r = _MetallicGlossMap.SampleLevel(sampler_MetallicGlossMap, uvMetallicGloss, 0).r;
    mg.g = color.a;
#else
    float2 uvMetallicGloss = TRANSFORM_TEX(uv, _MetallicGlossMap);
    mg = _MetallicGlossMap.SampleLevel(sampler_MetallicGlossMap, uvMetallicGloss, 0).ra;
#endif
    mg.g *= _GlossMapScale;
#else
    mg.r = _Metallic;
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    mg.g = color.a * _GlossMapScale;
#else
    mg.g = _Smoothness;
#endif
#endif
    return mg;
}

#ifdef _EMISSION
Texture2D _EmissionMap;
SamplerState sampler_EmissionMap;
float4 _EmissionMap_ST;

float4 _EmissionColor;

half3 Emission(float2 uv)
{
    float2 uvEmission = TRANSFORM_TEX(uv, _EmissionMap);
    return _EmissionMap.SampleLevel(sampler_EmissionMap, uvEmission, 0).rgb * _EmissionColor.rgb;
}
#endif

// from URP's Lighting.hlsl 
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

half OneMinusReflectivityMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}


[shader("closesthit")]
void URP_ClosestHit(inout RenderPayload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
{
#if !defined(_ALPHATEST_ON) && !defined(_ALPHABLEND_ON) && !defined(_ALPHAPREMULTIPLY_ON)
    // early reflection shadow check 
    if (payload.reflectionRayType == 1)
    {
        return;
    }
#endif

    Vertex v0, v1, v2;
    GetVertexData(v0, v1, v2);

    float2 barycentrics = attributes.barycentrics;
    float3 barycentricCoords = float3(1.0 - barycentrics.x - barycentrics.y, barycentrics.x, barycentrics.y);

    float3 position = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.position, v1.position, v2.position, barycentricCoords);
    float3 normal = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normal, v1.normal, v2.normal, barycentricCoords);
    float2 texcoord = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texcoord, v1.texcoord, v2.texcoord, barycentricCoords);

#ifdef RAYTRACEPROCEDURAL
        normal = attributes.normalOS;
#endif

    float2 uvMainTex = TRANSFORM_TEX(texcoord, _BaseMap);
    float4 color = _BaseMap.SampleLevel(sampler_BaseMap, uvMainTex, 0) * _BaseColor;
    
    // float3 worldNormal = mul(normal, (float3x3) WorldToObject3x4());
    float3 worldNormal = mul(ObjectToWorld3x4(), normal);
           worldNormal = normalize(worldNormal);

    float3 worldPosition = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();

    #if defined(_ALPHATEST_ON)
        if (color.a - _Cutoff < 0)
        {
            return;
        }
    
    #elif defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
        RayDesc ray;
        ray.TMin = 0.0;
        ray.TMax = _MaxRayDistance;
        ray.Direction = WorldRayDirection();
        ray.Origin = worldPosition + ray.Direction * 0.01;
    
        float originalIntensity = payload.rayIntensity;
        payload.rayIntensity = originalIntensity * saturate(1.0 - color.a);
    
        RenderPayload alphaBlendPayload = payload;
        TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, alphaBlendPayload);
        payload = alphaBlendPayload;
    
        payload.rayIntensity = originalIntensity * color.a;
    #endif

    // shadow check 
    if (payload.reflectionRayType == 1)
    {
        return;
    }

    half2 metallicGloss = MetallicGloss(texcoord, color);
    half metallic = metallicGloss.x;
    half smoothness = metallicGloss.y; // this is 1 minus the square root of real roughness m.

    // lighting 
    float shadowAttenuation = GetShadowAttenuation(worldPosition, payload.bounceCounter);
    float attenuation = GetAttenuation(worldNormal) * shadowAttenuation;
    color.rgb *= attenuation;

#ifdef _EMISSION
    color.rgb += Emission(texcoord);
#endif

#if defined(_CORGI_FOG)
    float zFog = RayTCurrent();
    UNITY_APPLY_FOG_RAW(zFog, color);
#endif

    // set final color 
    #ifndef _ALPHATEST_ON
        color.a = 1.0;
    #endif

    payload.color = color; 
    payload.worldPos = worldPosition;
    payload.worldNormal = normal;
}