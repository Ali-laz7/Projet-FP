Shader "Hidden/CommandBufferApplyRaytraceVolumetricLighting"
{
    HLSLINCLUDE

    #include "UnityCG.cginc"
    #include "UnityPBSLighting.cginc"
    #include "UnityStandardBRDF.cginc"
    #include "UnityStandardUtils.cginc"

    #pragma target 5.0

    struct AttributesDefault
    {
        float3 vertex : POSITION;
    };

    struct VaryingsDefault
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    SamplerState _LinearClamp;

    Texture2D _CameraDepthTexture;
    SamplerState sampler_CameraDepthTexture;

    Texture2D _RaytracedVolumetricLightingTexture;
    SamplerState sampler_RaytracedVolumetricLightingTexture;

    Texture2D _RaytracedGrabpass;
    SamplerState sampler_RaytracedGrabpass;

    float GetAttenuation(float3 worldPos, float3 worldNormal)
    {
        float attenuation = 1.0;
        float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
        attenuation *= saturate(dot(worldNormal, lightDir));
        return attenuation;
    }

    float2 ClampSampleUV(float2 uv)
    {
        return UnityStereoTransformScreenSpaceTex(saturate(uv));
    }

    float4x4 _CameraToWorld;
    float4x4 _InverseProjection;

    float3 GetWorldSpacePosition(float2 uv)
    {
        float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0).r;

        float4 clip = float4(2.0 * uv - 1.0, depth, 1.0);
        float4 viewPos = mul(_InverseProjection, clip);
        viewPos.xyz /= viewPos.w;
        viewPos.w = 1;

        float3 worldPos = mul(_CameraToWorld, viewPos).xyz;
        return worldPos; 
    }

    VaryingsDefault VertDefault(AttributesDefault v)
    {
        VaryingsDefault o;
        o.vertex = float4(v.vertex.xy, 0.0, 1.0);
        o.uv = (v.vertex.xy + 1.0) * 0.5;

        #if UNITY_UV_STARTS_AT_TOP
                o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

        o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

        return o;
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        // float3 worldPosition = GetWorldSpacePosition(i.uv);
        // float3 worldViewDir = normalize(worldPosition - _WorldSpaceCameraPos);

        float4 resolve = _RaytracedVolumetricLightingTexture.SampleLevel(sampler_RaytracedVolumetricLightingTexture, i.uv, 0.0);
        float4 color = _RaytracedGrabpass.Sample(sampler_RaytracedGrabpass, i.uv);

        float4 result = lerp(0, color, saturate(resolve.r));
        return result;
        // return resolve;
    }

    ENDHLSL

    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}