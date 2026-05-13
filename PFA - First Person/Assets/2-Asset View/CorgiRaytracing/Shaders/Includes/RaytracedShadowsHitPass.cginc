#ifndef RAYDATAHELPERSINCLUDED
#include "../Includes/RaytraceDataHelpers.cginc"
#include "../Includes/ShadowsPayload.cginc"
#endif

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

Texture2D _MainTex;
SamplerState sampler_MainTex;
float4 _MainTex_ST;

float4 _Color;

#if defined(_ALPHATEST_ON)
float _Cutoff;
#endif

[shader("closesthit")]
void Standard_ClosestHit_ShadowsGeneric(inout ShadowsPayload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
{

    Vertex v0, v1, v2;
    GetVertexData(v0, v1, v2);

    float2 barycentrics = attributes.barycentrics;
    float3 barycentricCoords = float3(1.0 - barycentrics.x - barycentrics.y, barycentrics.x, barycentrics.y);

    float3 normal = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normal, v1.normal, v2.normal, barycentricCoords);

    float2 texcoord = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texcoord, v1.texcoord, v2.texcoord, barycentricCoords);

    float2 uvMainTex = TRANSFORM_TEX(texcoord, _MainTex);
    float4 color = _MainTex.SampleLevel(sampler_MainTex, uvMainTex, 0) * _Color;

    float3 worldPosition = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();

	if (payload.rayType == 1.0)
	{
#if defined(_ALPHATEST_ON)
        float alpha = step(_Cutoff, color.a);
#elif defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
        float alpha = color.a;
#else
        float alpha = 1.0;
#endif

		float distanceTraveled = RayTCurrent();
        float scaleShadow = payload.rayIntensity * alpha; //  * saturate(distanceTraveled * 100);
		payload.light -= scaleShadow;
		return;
	}

    float3 worldNormal = mul(normal, (float3x3) WorldToObject3x4());

    float3 rayPosition = WorldRayOrigin() + WorldRayDirection() * RayTCurrent() + worldNormal * _ShadowBias;
    float3 rayDirection = _WorldSpaceLightPos0; //  reflect(worldDirection, normalize(normals.xyz * 2.0 - 1.0));

    Cast(rayPosition, rayDirection, payload);
}