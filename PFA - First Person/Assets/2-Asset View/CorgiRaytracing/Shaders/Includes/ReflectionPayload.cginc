
struct ReflectionPayload
{
	float4 color;
	float bounceCounter;
	float rayIntensity;

	int reflectionRayType; // 0 = reflection, 1 = shadow check 
};

float MAX_BOUNCES;
float MAX_BOUNCES_RIC;
int MAX_ROUGHNESS_COUNT;

float CAST_SHADOW_BOUNCE_CAP;

float3 _LightColor0;

inline float GetShadowAttenuation(float3 worldPos, float bounceCount)
{
    if (bounceCount >= CAST_SHADOW_BOUNCE_CAP)
    {
        return 1.0;
    }

    RayDesc ray;
    ray.Direction = normalize(_WorldSpaceLightPos0);
    ray.Origin = worldPos;
    ray.TMin = 0.001;
    ray.TMax = _MaxRayDistance;

    ReflectionPayload shadowsPayload;
    shadowsPayload.color = float4(0, 0, 0, 0);
    shadowsPayload.bounceCounter = bounceCount;
    shadowsPayload.rayIntensity = 1.0;
    shadowsPayload.reflectionRayType = 1;

    TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, shadowsPayload);

    return length(shadowsPayload.color - float4(0, 0, 0, 1)) < 0.001;;
}

// note, you have to pass the reference to the payload,
// passing just the value is not sufficient 
inline void BounceRay(float3 worldPosition, float viewDirection, float3 rayDirection, float gloss, float metallic, inout ReflectionPayload payload)
{
    payload.bounceCounter += 1.0;

    if (payload.bounceCounter < MAX_BOUNCES && gloss > 0.0125)
    {
        int max_ray_count = MAX_ROUGHNESS_COUNT;
        int ray_count = (int)((max_ray_count)*saturate(1.0 - gloss));
        ray_count = max(ray_count, 1);

        // gloss now fades intensity a bit (but only under 0.5) 
        payload.rayIntensity = saturate(gloss * 2 - 0.5);

        float4 averageColor = payload.color;

        for (int i = 0; i < ray_count; ++i)
        {
            float3 rOffset = GetRandom3D(worldPosition, rayDirection, 10, i + payload.bounceCounter);

            RayDesc ray;
            ray.TMin = 0.0;
            ray.TMax = _MaxRayDistance;
            ray.Direction = normalize(rayDirection + rOffset * (0.01 * saturate(1.0 - gloss)));
            ray.Origin = worldPosition + ray.Direction * 0.001;

            ReflectionPayload alphablendPayload = payload;
            TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, alphablendPayload);
            
            averageColor += alphablendPayload.color;
        }

        averageColor /= ray_count;
        payload.color = averageColor;
        
        if (payload.bounceCounter < 2)  
        {
            half oneMinusReflectivity;
            half3 specColor; 
            half3 albedo = payload.color.rgb;
            albedo = DiffuseAndSpecularFromMetallic(albedo, metallic, specColor, oneMinusReflectivity);
            payload.color.rgb = albedo.rgb;
            
            float3 giDiffuse = payload.color * saturate(dot(_WorldSpaceLightPos0, rayDirection));
            float3 giSpecular = specColor;
            
            payload.color = BRDF2_Unity_PBS(payload.color, specColor, oneMinusReflectivity, gloss, rayDirection, viewDirection, _WorldSpaceLightPos0, _LightColor0, giDiffuse, giSpecular);
        }
    }
} 