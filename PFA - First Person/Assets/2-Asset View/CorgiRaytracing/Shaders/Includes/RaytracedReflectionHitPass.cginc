#include "../Includes/RaytraceDataHelpers.cginc"
#include "../Includes/ReflectionPayload.cginc"

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

#pragma multi_compile _ _CORGI_FOG
#pragma multi_compile_fog 

Texture2D _MainTex;
SamplerState sampler_MainTex;
float4 _MainTex_ST;

#ifdef _METALLICGLOSSMAP
Texture2D _MetallicGlossMap;
SamplerState sampler_MetallicGlossMap;
float4 _MetallicGlossMap_ST; 
#endif

float4 _Color;
float _Glossiness;
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
    mg.g = _Glossiness;
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

[shader("closesthit")]
void Standard_ClosestHit_Generic(inout ReflectionPayload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
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

    float2 uvMainTex = TRANSFORM_TEX(texcoord, _MainTex);
    float4 color = _MainTex.SampleLevel(sampler_MainTex, uvMainTex, 0) * _Color;
    
    // float3 worldNormal = mul(normal, (float3x3) WorldToObject3x4());
    float3 worldNormal = mul(ObjectToWorld3x4(), normal);
           worldNormal = normalize(worldNormal);

    float3 worldPosition = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();

#if defined(_ALPHATEST_ON)
    if (color.a - _Cutoff < 0)
    {
        RayDesc ray;
        ray.TMin = 0.0;
        ray.TMax = _MaxRayDistance;
        ray.Direction = WorldRayDirection();
        ray.Origin = worldPosition + ray.Direction * 0.01;

        ReflectionPayload alphatestPayload = payload;
        TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, alphatestPayload);
        payload = alphatestPayload;

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

    ReflectionPayload alphablendPayload = payload;
    TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, alphablendPayload);
    payload = alphablendPayload;

    payload.rayIntensity = originalIntensity * color.a;
#endif

    // reflection shadow check 
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
    // color.a = 1-unityFogFactor;
#endif

#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    float alpha = color.a;
#else
    float alpha = 1.0;
#endif

    // add color from surface 
    if (payload.bounceCounter > 0)
    {
        // float distanceTraveled = RayTCurrent();
        // float intensity = 1.0; //  saturate(1.0 / (distanceTraveled * 0.5 + 1.0));

        float bounceIntensity = 1.0; //  saturate(1.0 - payload.bounceCounter * MAX_BOUNCES_RIC);
        payload.color = payload.color + color * (bounceIntensity * payload.rayIntensity * alpha);
    }

    float3 rayDirection = normalize(reflect(WorldRayDirection(), worldNormal));
    BounceRay(worldPosition, WorldRayDirection(), rayDirection, smoothness, metallic, payload);
}