#define RAYDATAHELPERSINCLUDED 1

#include "UnityRaytracingMeshUtils.cginc"

#if !IS_URP_RTX
    #include "HLSLSupport.cginc"
    #include "UnityShaderVariables.cginc"
    // #include "UnityCG.cginc" // errors?
#else

    // need to define this in URP
    float4 _WorldSpaceLightPos0;

    // from TextureXR.hlsl from HDRP 
    // note: modified this a bit to work with URP (cannot assign unity_StereoEyeIndex?)
    #if defined(STEREO_INSTANCING_ON) 
        #define UNITY_STEREO_INSTANCING_ENABLED
        #define USE_TEXTURE2D_X_AS_ARRAY
        #define USING_STEREO_MATRICES
    #endif

    #if defined(USE_TEXTURE2D_X_AS_ARRAY)
        #define XR_TEXTURE2D_X(textureName)                                      TEXTURE2D_ARRAY(textureName)
        #define XR_SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, XR_VIEW_INDEX, lod)

        #define RW_TEXTURE2D_X(type, textureName)                                RW_TEXTURE2D_ARRAY(type, textureName)
        #define COORD_TEXTURE2D_X(pixelCoord)                                    uint3(pixelCoord, XR_VIEW_INDEX)
    #else
        #define XR_TEXTURE2D_X(textureName)                                      TEXTURE2D(textureName)
        #define XR_SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)

        #define SAMPLE_TEXTURE2D_X_RTX(type, textureName)                        TEXTURE2D_ARRAY(type, textureName)
        #define RW_TEXTURE2D_X                                                   RW_TEXTURE2D
        #define COORD_TEXTURE2D_X(pixelCoord)                                    pixelCoord
    #endif

    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex) uint XR_VIEW_INDEX = viewIndex
        #define UNITY_XR_FALLBACK_VIEW_INDEX(viewIndex) uint SLICE_ARRAY_INDEX = viewIndex
    #else
        #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex) uint XR_VIEW_INDEX = 0
        #define UNITY_XR_FALLBACK_VIEW_INDEX(viewIndex) uint SLICE_ARRAY_INDEX = 0
    #endif

    // Backward compatibility
    #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX   UNITY_XR_ASSIGN_VIEW_INDEX


#endif

float4x4 _Array_InverseProjection[2];
float4x4 _InverseProjection;

#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define GET_INVERSE_PROJECTION _Array_InverseProjection[XR_VIEW_INDEX]
#else
    #define GET_INVERSE_PROJECTION _InverseProjection
#endif

#define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

RaytracingAccelerationStructure _RaytracingAccelerationStructure : register(t0);

#if !defined(TRANSFORM_TEX)
    #define TRANSFORM_TEX_ST(tex,namest) (tex.xy * namest.xy + namest.zw)
    #define TRANSFORM_TEX(tex,name) TRANSFORM_TEX_ST(tex, name##_ST)
#endif

// debug defines 
// #define _RaytraceAgainstLayers 0xFFFFFFF
// #define _RayFlags RAY_FLAG_CULL_BACK_FACING_TRIANGLES
// #define _MaxRayDistance 1000

uint _RayFlags;
uint _RaytraceAgainstLayers;
float _MaxRayDistance;

// float3 _WorldSpaceCameraPos;

// Vertex and Payload data 
struct AttributeData
{
    float2 barycentrics;
    
#ifdef RAYTRACEPROCEDURAL
    float3 normalOS;
#endif
};

struct Vertex
{
    float3 position;
    float3 normal;
    float2 texcoord;
};

Vertex FetchVertex(uint vertexIndex)
{
    Vertex v;
    v.position = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
    v.normal = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
    v.texcoord = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
    return v;
}

// helper functions 
float GetAttenuation(float3 worldNormal)
{
    float attenuation = 1.0;
    float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
    attenuation *= saturate(dot(worldNormal, lightDir));

    return attenuation;
}

void GetVertexData(inout Vertex v0, inout Vertex v1, inout Vertex v2)
{
    uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
    v0 = FetchVertex(triangleIndices.x);
    v1 = FetchVertex(triangleIndices.y);
    v2 = FetchVertex(triangleIndices.z);
}

// math helpers 
float RandomValue1D(int x)
{
    int frequency = 10000;
    float result = sin(x + 100);

    result *= frequency;
    result = fmod(result, 1.0);

    return result;
}

float RandomValue1DLerp(float position)
{
    int intPosition = (int)floor(position);
    float fractionalPosition = fmod(position, 1.0);
    fractionalPosition = smoothstep(0.0, 1.0, fractionalPosition);

    float r0 = RandomValue1D(intPosition);
    float r1 = RandomValue1D(intPosition + 1);
    return lerp(r0, r1, fractionalPosition) * 2.0 - 1.0;
}

float3 GetRandom3D(float3 worldPos, float3 worldDirection, float scale, float offset)
{
	float r_m = RandomValue1DLerp( (length(worldPos.xyz) + length(worldDirection.xyz)) * scale + offset				+ 1000);
	float r_x = RandomValue1DLerp( (worldPos.x + worldDirection.x) * scale + offset + r_m		+ 2000);
	float r_y = RandomValue1DLerp( (worldPos.y + worldDirection.y) * scale + offset + r_x		+ 3000);
	float r_z = RandomValue1DLerp( (worldPos.z + worldDirection.z) * scale + offset + r_x + r_y + 4000);

	return normalize(float3(r_x, r_y, r_z)) * r_m;
}

// from UnityCG.cginc 
inline float Linear01Depth(float z)
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

inline float LinearEyeDepth(float z)
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}

#ifdef UNITY_COLORSPACE_GAMMA
    #define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
    #define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
    #define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
    #define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
    #else // Linear values
    #define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
    #define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
    #define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
    #define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
#endif

#if defined(UNITY_REVERSED_Z)
    #if UNITY_REVERSED_Z == 1
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #else
        //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = (coord) * unity_FogParams.z + unity_FogParams.w
#elif defined(FOG_EXP)
    // factor = exp(-density*z)
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.y * (coord); unityFogFactor = exp2(-unityFogFactor)
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.x * (coord); unityFogFactor = exp2(-unityFogFactor*unityFogFactor)
#else
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = 0.0
#endif


#define UNITY_FOG_LERP_COLOR(col,fogCol,fogFac) col.rgb = lerp((fogCol).rgb, (col).rgb, saturate(fogFac))
#define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE(coord))
#define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) UNITY_CALC_FOG_FACTOR((coord).x); UNITY_FOG_LERP_COLOR(col,fogCol,unityFogFactor)
#define UNITY_APPLY_FOG_COLOR_RAW(coord,col,fogCol) UNITY_CALC_FOG_FACTOR_RAW((coord).x); UNITY_FOG_LERP_COLOR(col,fogCol,unityFogFactor)
#define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE(coord))

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,unity_FogColor)
    #define UNITY_APPLY_FOG_RAW(coord,col) UNITY_APPLY_FOG_COLOR_RAW(coord,col,unity_FogColor)
#else
    #define UNITY_APPLY_FOG(coord,col) 
    #define UNITY_APPLY_FOG_RAW(coord,col) 
#endif

// from UnityCG.cginc
#ifdef UNITY_COLORSPACE_GAMMA
#define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
#define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
#else // Linear values
#define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
#define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
#define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
#endif

#define UNITY_INV_PI        0.31830988618f

// from UnityStandardUtils.cginc
inline half OneMinusReflectivityFromMetallic(half metallic)
{
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline half3 DiffuseAndSpecularFromMetallic(half3 albedo, half metallic, out half3 specColor, out half oneMinusReflectivity)
{
    specColor = lerp(unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
    oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
    return albedo * oneMinusReflectivity;
}

// from UnityStandardBRDF.cginc
inline half Pow4(half x)
{
    return x * x * x * x;
}

inline float2 Pow4(float2 x)
{
    return x * x * x * x;
}

inline half3 Pow4(half3 x)
{
    return x * x * x * x;
}

inline half4 Pow4(half4 x)
{
    return x * x * x * x;
}

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

half RoughnessToPerceptualRoughness(half roughness)
{
    return sqrt(roughness);
}

// Smoothness is the user facing name
// it should be perceptualSmoothness but we don't want the user to have to deal with this name
half SmoothnessToRoughness(half smoothness)
{
    return (1 - smoothness) * (1 - smoothness);
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
    return (1 - smoothness);
}

inline float GGXTerm(float NdotH, float roughness)
{
    float a2 = roughness * roughness;
    float d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
    return UNITY_INV_PI * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
                                            // therefore epsilon is smaller than what can be represented by half
}

inline half PerceptualRoughnessToSpecPower(half perceptualRoughness)
{
    half m = PerceptualRoughnessToRoughness(perceptualRoughness);   // m is the true academic roughness.
    half sq = max(1e-4f, m * m);
    half n = (2.0 / sq) - 2.0;                          // https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
    n = max(n, 1e-4f);                                  // prevent possible cases of pow(0,0), which could happen when roughness is 1.0 and NdotH is zero
    return n;
}

// approximage Schlick with ^4 instead of ^5
inline half3 FresnelLerpFast(half3 F0, half3 F90, half cosA)
{
    half t = Pow4(1 - cosA);
    return lerp(F0, F90, t);
}

#define UNITY_BRDF_GGX 1

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) BlinnPhong
//  b) [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half4 BRDF2_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
    float3 normal, float3 viewDir, float3 lightDir, float3 lightColor, float3 giDiffuse, float3 giSpecular)
{
    float3 halfDir = normalize(float3(lightDir)+viewDir);

    half nl = saturate(dot(normal, lightDir));
    float nh = saturate(dot(normal, halfDir));
    half nv = saturate(dot(normal, viewDir));
    float lh = saturate(dot(lightDir, halfDir));

    // Specular term
    half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

#if UNITY_BRDF_GGX

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    float a = roughness;
    float a2 = a * a;

    float d = nh * nh * (a2 - 1.f) + 1.00001f;
#ifdef UNITY_COLORSPACE_GAMMA
    // Tighter approximation for Gamma only rendering mode!
    // DVF = sqrt(DVF);
    // DVF = (a * sqrt(.25)) / (max(sqrt(0.1), lh)*sqrt(roughness + .5) * d);
    float specularTerm = a / (max(0.32f, lh) * (1.5f + roughness) * d);
#else
    float specularTerm = a2 / (max(0.1f, lh * lh) * (roughness + 0.5f) * (d * d) * 4);
#endif

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4f;
#endif

#else

    // Legacy
    half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
    // Modified with approximate Visibility function that takes roughness into account
    // Original ((n+1)*N.H^n) / (8*Pi * L.H^3) didn't take into account roughness
    // and produced extremely bright specular at grazing angles

    half invV = lh * lh * smoothness + perceptualRoughness * perceptualRoughness; // approx ModifiedKelemenVisibilityTerm(lh, perceptualRoughness);
    half invF = lh;

    half specularTerm = ((specularPower + 1) * pow(nh, specularPower)) / (8 * invV * invF + 1e-4h);

#ifdef UNITY_COLORSPACE_GAMMA
    specularTerm = sqrt(max(1e-4f, specularTerm));
#endif

#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(realRoughness^2+1)

    // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
    // 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
#ifdef UNITY_COLORSPACE_GAMMA
    half surfaceReduction = 0.28;
#else
    half surfaceReduction = (0.6 - 0.08 * perceptualRoughness);
#endif

    surfaceReduction = 1.0 - roughness * perceptualRoughness * surfaceReduction;

    half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
    half3 color = (diffColor + specularTerm * specColor) * lightColor * nl
        + giDiffuse * diffColor
        + surfaceReduction * giSpecular * FresnelLerpFast(specColor, grazingTerm, nv);

    return half4(color, 1);
} 