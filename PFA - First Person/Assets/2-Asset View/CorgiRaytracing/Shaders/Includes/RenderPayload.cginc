
struct RenderPayload
{
	float4 color;
	float bounceCounter;
	float rayIntensity;

    float3 worldPos; 
    float3 worldNormal; 
	int reflectionRayType; // 0 = reflection, 1 = shadow check 
};

float MAX_BOUNCES;
float MAX_BOUNCES_RIC;
int MAX_ROUGHNESS_COUNT;

float CAST_SHADOW_BOUNCE_CAP;

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

    RenderPayload shadowsPayload = (RenderPayload) 0;
    shadowsPayload.color = float4(0, 0, 0, 0);
    shadowsPayload.bounceCounter = bounceCount;
    shadowsPayload.rayIntensity = 1.0;
    shadowsPayload.reflectionRayType = 1;

    TraceRay(_RaytracingAccelerationStructure, _RayFlags, _RaytraceAgainstLayers, 0, 1, 0, ray, shadowsPayload);

    return length(shadowsPayload.color - float4(0, 0, 0, 1)) < 0.001;;
}