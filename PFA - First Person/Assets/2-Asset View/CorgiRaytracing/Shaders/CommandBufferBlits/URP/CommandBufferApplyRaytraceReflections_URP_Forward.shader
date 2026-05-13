Shader "Hidden/CommandBufferApplyRaytraceReflections_URP_Forward"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #pragma target 5.0

    float4 _WorldSpaceLightPos0;

    struct AttributesDefault
    {
        float3 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct VaryingsDefault
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    SamplerState _LinearClamp;

    TEXTURE2D_X(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);

    TEXTURE2D_X(_RTXReflectionsTex);
    SAMPLER(sampler_RTXReflectionsTex);

    TEXTURE2D_X(_ReflectionsGrabpass);
    SAMPLER(sampler_ReflectionsGrabpass);

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
        float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

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

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = float4(v.vertex.xy, 0.0, 1.0);
        o.uv = (v.vertex.xy + 1.0) * 0.5;

        #if UNITY_UV_STARTS_AT_TOP
                o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

        return o;
    }

    float4 ObjectToClipPos(float3 pos)
    {
        return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4 (pos, 1)));
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float4 resolve = SAMPLE_TEXTURE2D_X(_RTXReflectionsTex, sampler_RTXReflectionsTex, i.uv);
        //resolve.a = saturate(resolve.a);

        // float3 worldPosition = GetWorldSpacePosition(i.uv);
        // float3 worldViewDir = normalize(worldPosition - _WorldSpaceCameraPos);

        float4 color = SAMPLE_TEXTURE2D_X(_ReflectionsGrabpass, sampler_ReflectionsGrabpass, i.uv);

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        float z = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, saturate(UnityStereoTransformScreenSpaceTex(i.uv))).r;
        float3 worldPos = GetWorldSpacePosition(i.uv);
        float3 clipPos = ObjectToClipPos(worldPos);
        float unityFogFactor = ComputeFogFactor(clipPos.z);

        // float d = (z) * _ProjectionParams.z;
        // UNITY_CALC_FOG_FACTOR(d);

        float emissionFactor = saturate(unityFogFactor);
        resolve.a *= emissionFactor;
#endif

        color.rgb += resolve.rgb * resolve.a;
        //color.rgb = lerp(color.rgb, resolve.rgb, resolve.a);

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

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