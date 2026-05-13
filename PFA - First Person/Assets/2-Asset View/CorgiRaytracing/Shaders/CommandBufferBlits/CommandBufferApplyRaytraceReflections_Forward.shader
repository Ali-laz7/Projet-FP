Shader "Hidden/CommandBufferApplyRaytraceReflections_Forward"
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

    Texture2D _RTXReflectionsTex;
    SamplerState sampler_RTXReflectionsTex;

    Texture2D _ReflectionGrabpass;
    SamplerState sampler_ReflectionGrabpass;

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

    float4x4 corgi_CameraToWorld;
    float4x4 corgi_InverseProjection;

    float3 GetWorldSpacePosition(float2 uv)
    {
        float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0).r;

        float4 clip = float4(2.0 * uv - 1.0, depth, 1.0);
        float4 viewPos = mul(corgi_InverseProjection, clip);
        viewPos.xyz /= viewPos.w;
        viewPos.w = 1;

        float3 worldPos = mul(corgi_CameraToWorld, viewPos).xyz;
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
        float4 resolve = _RTXReflectionsTex.SampleLevel(sampler_RTXReflectionsTex, i.uv, 0.0);
        //resolve.a = saturate(resolve.a);

        // float3 worldPosition = GetWorldSpacePosition(i.uv);
        // float3 worldViewDir = normalize(worldPosition - _WorldSpaceCameraPos);

        float4 color = _ReflectionGrabpass.Sample(sampler_ReflectionGrabpass, i.uv);

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        float z = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, saturate(i.uv), 0).r;
        float3 worldPos = GetWorldSpacePosition(i.uv);
        float3 clipPos = UnityObjectToClipPos(worldPos);
        UNITY_CALC_FOG_FACTOR(clipPos.z);

        // float d = (z) * _ProjectionParams.z;
        // UNITY_CALC_FOG_FACTOR(d);

        float emissionFactor = saturate(unityFogFactor);
        resolve.a *= emissionFactor;
#endif

        color.rgb += resolve.rgb * resolve.a;

        return color;
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
                #pragma multi_compile_fog
                #pragma multi_compile_instancing

            ENDHLSL
        }
    }
}