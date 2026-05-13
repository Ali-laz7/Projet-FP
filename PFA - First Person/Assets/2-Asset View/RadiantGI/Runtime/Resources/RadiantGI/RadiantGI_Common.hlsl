#ifndef RGI_COMMON
#define RGI_COMMON

	// Copyright 2022 Kronnect - All Rights Reserved.
    #include "RadiantGI_Common_URP2BuiltIn.hlsl"	

    // Common macros
    TEXTURE2D_X(_MainTex);
    float4 _MainTex_TexelSize;
    float4 _MainTex_ST;

    TEXTURE2D(_NoiseTex);
    float4 _NoiseTex_TexelSize;

	TEXTURE2D_X(_MotionVectorTexture);
    TEXTURE2D_X(_GBuffer0);
    TEXTURE2D_X(_GBuffer1);

    #define dot2(x) dot(x, x)
    
    TEXTURE2D_X(_CameraDepthTexture);
    float4 _CameraDepthTexture_TexelSize;

    TEXTURE2D_X(_DownscaledDepthRT);
    float4 _DownscaledDepthRT_TexelSize;

    float4x4 _WorldToViewDir;
    float4x4 _ViewToWorldDir;
    float4x4 _InvViewProjection;

	float4 _SourceSize;
	#define SOURCE_SIZE _SourceSize.xy
	#define GOLDEN_RATIO_ACUM _SourceSize.z
    #define FRAME_NUMBER _SourceSize.w

    float4 _IndirectData;
    #define INDIRECT_INTENSITY _IndirectData.x
    #define INDIRECT_MAX_BRIGHTNESS _IndirectData.y
    #define INDIRECT_DISTANCE_ATTENUATION _IndirectData.z
    #define RAY_REUSE_INTENSITY _IndirectData.w

    float4 _RayData;
    #define RAY_COUNT _RayData.x
    #define RAY_MAX_LENGTH _RayData.y
    #define RAY_MAX_SAMPLES _RayData.z
    #define THICKNESS _RayData.w

    float3 _TemporalData;
    #define TEMPORAL_RESPONSE_SPEED _TemporalData.x
    #define TEMPORAL_MAX_DEPTH_DIFFERENCE _TemporalData.y
    #define TEMPORAL_CHROMA_THRESHOLD _TemporalData.z

    float4 _ExtraData;
    #define JITTER_AMOUNT _ExtraData.x
    #define BLUR_SPREAD _ExtraData.y
    #define NORMALS_INFLUENCE _ExtraData.z
    #define LUMA_INFLUENCE _ExtraData.w

    half4  _ExtraData2;
    #define LUMA_THRESHOLD _ExtraData2.x
    #define LUMA_MAX _ExtraData2.y
    #define COLOR_SATURATION _ExtraData2.z
    #define RSM_INTENSITY _ExtraData2.w

    half4  _ExtraData3;
    #define NEAR_CAMERA_ATTENUATION _ExtraData3.z
    #define NEAR_FIELD_OBSCURANCE_SPREAD _ExtraData3.y
    #define NEAR_FIELD_OBSCURANCE_INTENSITY _ExtraData3.w

    half3  _ExtraData4;
    #define NEAR_FIELD_OBSCURANCE_MAX_CAMERA_DISTANCE _ExtraData4.x
    #define NEAR_FIELD_OBSCURANCE_OCCLUDER_DISTANCE _ExtraData4.y

    half3 _NFOTint;
    #define NEAR_FIELD_OBSCURANCE_TINT _NFOTint

    float4 _BoundsXZ;

    half4 _ExtraData5;
    #define SPECULAR_CONTRIBUTION _ExtraData5.x
    #define DOWNSAMPLING _ExtraData5.y
    #define SOURCE_BRIGHTNESS _ExtraData5.z
    #define GI_WEIGHT _ExtraData5.w    
    
    #define UNITY_MATRIX_I_VP _InvViewProjection
    #define PI 3.1415927

	
	struct AttributesFS {
		float4 positionHCS : POSITION;
		float2 uv          : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

 	struct VaryingsRGI {
    	float4 positionCS : SV_POSITION;
    	float2 uv  : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
	};

	VaryingsRGI VertRGI(AttributesFS input) {
	    VaryingsRGI output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = UnityObjectToClipPos(input.positionHCS);
        output.uv = input.uv;
    	return output;
	}

    float GetRawDepth(float2 uv) {
        float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
        return depth;
    }

    float RawToLinearEyeDepth(float rawDepth) {
        float eyeDepth = LinearEyeDepth(rawDepth);
        #if _ORTHO_SUPPORT
            #if UNITY_REVERSED_Z
                rawDepth = 1.0 - rawDepth;
            #endif
            float orthoEyeDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
            eyeDepth = lerp(eyeDepth, orthoEyeDepth, unity_OrthoParams.w);
        #endif
        return eyeDepth;
    }

    float GetLinearEyeDepth(float2 uv) {
        float rawDepth = GetRawDepth(uv);
        return RawToLinearEyeDepth(rawDepth);
    }

    float GetDownscaledRawDepth(float2 uv) {
        float depth = SAMPLE_TEXTURE2D_X_LOD(_DownscaledDepthRT, sampler_PointClamp, uv, 0).r;
        return depth;
    }

    float GetLinearEyeDownscaledDepth(float2 uv) {
        float rawDepth = GetDownscaledRawDepth(uv);
        return RawToLinearEyeDepth(rawDepth);
    }


float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

#if UNITY_UV_STARTS_AT_TOP
    // Our world space, view space, screen space and NDC space are Y-up.
    // Our clip space is flipped upside-down due to poor legacy Unity design.
    // The flip is baked into the projection matrix, so we only have to flip
    // manually when going from CS to NDC and back.
    positionCS.y = -positionCS.y;
#endif

    return positionCS;
}


float3 ComputeViewSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invProjMatrix)
{
    float4 positionCS = ComputeClipSpacePosition(positionNDC, deviceDepth);
    float4 positionVS = mul(invProjMatrix, positionCS);
    // The view space uses a right-handed coordinate system.
    positionVS.z = -positionVS.z;
    return positionVS.xyz / positionVS.w;
}


float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
{
    float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
    float4 hpositionWS = mul(invViewProjMatrix, positionCS);
    return hpositionWS.xyz / hpositionWS.w;
}


    float3 GetWorldPosition(float2 uv, float rawDepth) {

         #if UNITY_REVERSED_Z
              float depth = rawDepth;
         #else
              float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
         #endif

         // Reconstruct the world space positions.
         float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

        return worldPos;
    }


    float3 GetWorldPosition(float2 uv) {
        float rawDepth = GetRawDepth(uv);
        return GetWorldPosition(uv, rawDepth);
    }

#if _FORWARD_AUTONORMALS

    float3 GetViewPos(float2 uv) {
       float3 viewSpaceRay = mul(unity_CameraInvProjection, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
       return viewSpaceRay * GetLinearEyeDepth(uv);
    }

    half3 SampleSceneNormals(float2 uv) {
       half3 vr = GetViewPos(uv + _CameraDepthTexture_TexelSize.xy * float2( 1.0, 0.0));
       half3 vl = GetViewPos(uv + _CameraDepthTexture_TexelSize.xy * float2(-1.0, 0.0));
       half3 vb = GetViewPos(uv + _CameraDepthTexture_TexelSize.xy * float2( 0.0,-1.0));
       half3 vt = GetViewPos(uv + _CameraDepthTexture_TexelSize.xy * float2( 0.0, 1.0));
       half3 hDeriv = vr - vl;
       half3 vDeriv = vt - vb;
       half3 viewNormal = normalize(cross(hDeriv, vDeriv));
       return viewNormal;
    }

	half3 GetWorldNormal(float2 uv) {
		half3 normalVS = SampleSceneNormals(uv);
        half3 normalWS = mul((float3x3)_ViewToWorldDir, normalVS);
		return normalWS;
	}

    void GetViewAndWorldNormal(float2 uv, out float3 normalVS, out float3 normalWS) {
        normalVS = SampleSceneNormals(uv);
        normalWS = mul((float3x3)_ViewToWorldDir, normalVS);
        normalVS.z *= -1.0;
	}

#elif _FORWARD

    TEXTURE2D_X(_CameraDepthNormalsTexture);
    float4 _CameraDepthNormalsTexture_TexelSize;

    half3 SampleSceneNormals(float2 uv) {
        float4 depthNormal = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthNormalsTexture, sampler_PointClamp, uv, 0);
        float3 normalVS = DecodeViewNormalStereo(depthNormal);
        return normalVS;
    }

	half3 GetWorldNormal(float2 uv) {
		half3 normalVS = SampleSceneNormals(uv);
        half3 normalWS = mul((float3x3)_ViewToWorldDir, normalVS);
		return normalWS;
	}

    void GetViewAndWorldNormal(float2 uv, out float3 normalVS, out float3 normalWS) {
        float4 depthNormal = SAMPLE_TEXTURE2D_X(_CameraDepthNormalsTexture, sampler_PointClamp, uv);
        normalVS = DecodeViewNormalStereo(depthNormal);
        normalWS = mul((float3x3)_ViewToWorldDir, normalVS);
        normalVS.z *= -1.0;
	}

#else

    TEXTURE2D_X(_CameraGBufferTexture2);
    float4 _CameraGBufferTexture2_TexelSize;

    half3 SampleSceneNormals(float2 uv) {
        half4 depthNormal = SAMPLE_TEXTURE2D_X_LOD(_CameraGBufferTexture2, sampler_PointClamp, uv, 0);
        return normalize(depthNormal.xyz * 2.0 - 1.0);;
    }

	half3 GetWorldNormal(float2 uv) {
		half3 normalWS = SampleSceneNormals(uv);
		return normalWS;
	}

    void GetViewAndWorldNormal(float2 uv, out float3 normalVS, out float3 normalWS) {
        normalWS = SampleSceneNormals(uv);
        normalVS = mul((float3x3)_WorldToViewDir, normalWS);
        normalVS.z *= -1.0;
	}
#endif

    half2 GetVelocity(float2 uv) {
		half2 mv = SAMPLE_TEXTURE2D_X_LOD(_MotionVectorTexture, sampler_PointClamp, uv, 0).xy;
        return mv;
    }

    half GetLuma(half3 rgb) {
        const half3 lum = half3(0.299, 0.587, 0.114);
        return dot(rgb, lum);
    }

float InterleavedGradientNoise(float2 pixCoord, int frameCount)
{
    const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
    float2 frameMagicScale = float2(2.083f, 4.867f);
    pixCoord += frameCount * frameMagicScale;
    return frac(magic.z * frac(dot(pixCoord, magic.xy)));
}

float3 GetCameraPositionWS() {
    return _WorldSpaceCameraPos.xyz;
}

    bool IsSkyBox(float rawDepth) {
        #if UNITY_REVERSED_Z
            return rawDepth <= 0;
		#else
            return rawDepth >= 1.0;
		#endif
    }

    bool IsOutsideBounds(float3 wpos) {
        return any(wpos.xz < _BoundsXZ.xy) || any(wpos.xz > _BoundsXZ.zw);
    }

#endif // RGI_COMMON