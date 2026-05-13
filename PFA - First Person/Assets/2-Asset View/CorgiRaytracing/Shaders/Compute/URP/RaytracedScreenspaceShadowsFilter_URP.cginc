// #include "HLSLSupport.cginc"
// #include "UnityShaderVariables.cginc"
// #include "UnityShadowLibrary.cginc"
// #include "UnityCG.cginc"

// need to define this in URP
float4 _WorldSpaceLightPos0;

// from TextureXR.hlsl from HDRP 
// note: modified this a bit to work with URP (cannot assign unity_StereoEyeIndex?)
#if defined(CORGI_SPI_ENABLED)
    #if (defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)
        #define UNITY_TEXTURE2D_X_ARRAY_SUPPORTED
    #endif

    #if defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED) && !defined(DISABLE_TEXTURE2D_X_ARRAY)
        #define USE_TEXTURE2D_X_AS_ARRAY
    #endif

    #if defined(STEREO_INSTANCING_ON) && defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
        #define UNITY_STEREO_INSTANCING_ENABLED
    #endif

    #if defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED) && (defined(SHADER_STAGE_COMPUTE) || defined(SHADER_STAGE_RAY_TRACING))
        #define UNITY_STEREO_INSTANCING_ENABLED
    #endif

    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define USING_STEREO_MATRICES
    #endif
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) 

    #define RW_TEXTURE2D_X(type, textureName)                                RW_TEXTURE2D_ARRAY(type, textureName)
    #define COORD_TEXTURE2D_X(pixelCoord)                                    uint3(pixelCoord, XR_VIEW_INDEX)
#else
    #define RW_TEXTURE2D_X                                                   RW_TEXTURE2D
    #define COORD_TEXTURE2D_X(pixelCoord)                                    pixelCoord
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex) uint XR_VIEW_INDEX = viewIndex
#else
    #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex) uint XR_VIEW_INDEX = 0
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define INSTANCINGARG ,XR_VIEW_INDEX
#else
    #define INSTANCINGARG 
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X(_CameraDepthTexture);
TEXTURE2D_X(_ShadowmapTemp);
RW_TEXTURE2D_X(float, _ShadowMap);

SamplerComparisonState _LinearClampCompare;
SamplerState _LinearClamp;

float4 _ShadowmapTemp_TexelSize;

float _ShadowStrength;
int _textureWidth;
int _textureHeight;

float SampleShadowMap(uint2 uv
    #ifdef UNITY_STEREO_INSTANCING_ENABLED
    ,uint XR_VIEW_INDEX
    #endif
    )
{
    // float2 uvf = float2(uv * _ShadowmapTemp_TexelSize.zw);
    // float depth = _ShadowmapTemp.SampleLevel(_LinearClamp, uvf, 0).r;
    // return depth; 

    uv.x = clamp(uv.x, 1, _textureWidth - 2);
    uv.y = clamp(uv.y, 1, _textureHeight - 2);


    return _ShadowmapTemp[COORD_TEXTURE2D_X(uv)];
}

float CorgiSampleShadow(float3 uv
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    , uint XR_VIEW_INDEX
#endif
)
{
    return 1.0;
	// return 1.0 - _ShadowmapTemp.SampleCmpLevelZero(_LinearClampCompare, saturate(uv.xy), uv.z);
}

float SampleShadowMap_3x3(int2 uv
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    , uint XR_VIEW_INDEX
#endif
)
{
    float shadow = 1;

    shadow = 0;
    shadow += SampleShadowMap(uv + int2(-1, -1)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(0, -1)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(1, -1)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(-1, 0)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(0, 0)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(1, 0)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(-1, 1)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(0, 1)INSTANCINGARG);
    shadow += SampleShadowMap(uv + int2(1, 1)INSTANCINGARG);
    shadow /= 9.0;

    return shadow;
}

float SampleShadowMap_Variable(int2 uv
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    , uint XR_VIEW_INDEX
#endif
)
{

	// increase this value for more expensive shadow filtering
	const int iwidth = 4; 

    float shadow = 0.0;
    float counter = 0;

    for (int x = -iwidth; x <= iwidth; ++x)
    for (int y = -iwidth; y <= iwidth; ++y)
    {
        counter++;
        shadow += SampleShadowMap(uv + int2(x, y)INSTANCINGARG);
    }

    shadow /= counter;

    return shadow;
}

[numthreads(32, 32, 1)]
void SmoothShadows(int3 id : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(id.z);

    // bounds check :( 
    if (id.x >= _textureWidth || id.y >= _textureHeight)
    {
        return;
    }

    float shadows = SampleShadowMap_Variable(id.xy
        #ifdef UNITY_STEREO_INSTANCING_ENABLED
        ,XR_VIEW_INDEX
        #endif
    );

	// uncomment for unity-like soft shadows 
	// float4 uv = float4(id.xy * _ShadowmapTemp_TexelSize.xy, 0, 1);
	// float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv.xy, 0).r;
	// uv.z = depth;
	// 
	// float shadows = Corgi_SampleShadowmap_PCF7x7Tent(uv, 0);

    shadows = 1.0 - shadows;
    shadows *= _ShadowStrength;
    shadows = 1.0 - shadows; 

    _ShadowMap[COORD_TEXTURE2D_X(id.xy)] = shadows;
}

[numthreads(32, 32, 1)]
void HardShadows(int3 id : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(id.z);

    // bounds check :( 
    if (id.x >= _textureWidth || id.y >= _textureHeight)
    {
        return;
    }

    int2 uv = id.xy;
    float shadows = SampleShadowMap(uv
#ifdef UNITY_STEREO_INSTANCING_ENABLED
        , XR_VIEW_INDEX
#endif
    );

    shadows = 1.0 - shadows;
    shadows *= _ShadowStrength;
    shadows = 1.0 - shadows;

    _ShadowMap[COORD_TEXTURE2D_X(id.xy)] = shadows;
}

[numthreads(32, 32, 1)]
void VolumetricSmoothShadows(int3 id : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(id.z);

    // bounds check :( 
    if (id.x >= _textureWidth || id.y >= _textureHeight)
    {
        return;
    }

    float shadows = SampleShadowMap_Variable(id.xy
#ifdef UNITY_STEREO_INSTANCING_ENABLED
        , XR_VIEW_INDEX
#endif
    );
    //float shadows = SampleShadowMap(uv);

    shadows = 1.0 - shadows;
    shadows *= _ShadowStrength;
    shadows = 1.0 - shadows;

    _ShadowMap[COORD_TEXTURE2D_X(id.xy)] = shadows;
} 