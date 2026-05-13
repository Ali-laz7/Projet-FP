Shader "Hidden/CommandBufferApplyRaytraceReflections_Deferred"
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

    Texture2D<float4> _CameraGBufferTexture0;
    Texture2D<float4> _CameraGBufferTexture1;
    Texture2D<float4> _CameraGBufferTexture2;
    Texture2D<float4> _CameraGBufferTexture3;
    
    // Texture2D<float4> corgi_CameraGBufferTexture0_Grabpass; 
    // Texture2D<float4> corgi_CameraGBufferTexture1_Grabpass; 
    // Texture2D<float4> corgi_CameraGBufferTexture2_Grabpass; 
    // Texture2D<float4> corgi_CameraGBufferTexture3_Grabpass; 

    SamplerState _LinearClamp;

    Texture2D _CameraDepthTexture;
    SamplerState sampler_CameraDepthTexture;

    Texture2D _RTXReflectionsTex;
    SamplerState sampler_RTXReflectionsTex;

#ifdef _ReflectionsNeedVL
    Texture2D _RaytracedVolumetricLightingTexture;
    SamplerState sampler_RaytracedVolumetricLightingTexture;
#endif

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

    float3 GetViewSpacePosition(float2 uv)
    {
        float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0).r;

        float4 clip = float4(2.0 * uv - 1.0, depth, 1.0);
        float4 viewPos = mul(corgi_InverseProjection, clip);
        viewPos.xyz /= viewPos.w;
        viewPos.w = 1;

        return viewPos;
    }


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

        // o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

        return o;
    }

    struct FragmentOutput
    {
        float4 gbuffer0 : SV_Target0;
    };

    FragmentOutput Frag(VaryingsDefault i)
    {
        float z = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, saturate(i.uv), 0).r;

        if (Linear01Depth(z) > 0.999)
        {
            return (FragmentOutput) 0;
        }

        float4 gbuffer0 = _CameraGBufferTexture0.Sample(_LinearClamp, i.uv);
        float4 gbuffer1 = _CameraGBufferTexture1.Sample(_LinearClamp, i.uv);
        float4 gbuffer2 = _CameraGBufferTexture2.Sample(_LinearClamp, i.uv);
        float4 gbuffer3 = _CameraGBufferTexture3.Sample(_LinearClamp, i.uv);


        if (gbuffer1.a < 0.01)
        {
            return (FragmentOutput)0;
        }

        float oneMinusReflectivity = 0.0;
        EnergyConservationBetweenDiffuseAndSpecular(gbuffer0.rgb, gbuffer1.rgb, oneMinusReflectivity);

        float3 normal = 2.0 * gbuffer2.rgb - 1.0;

		float4 resolve = _RTXReflectionsTex.SampleLevel(sampler_RTXReflectionsTex, saturate(i.uv), (SmoothnessToRoughness(gbuffer1.a) * 6));

        UnityLight light;
        light.color = 0.0;
        light.dir = 0.0;
        light.ndotl = 0.0;

        UnityIndirect indirect;
        indirect.diffuse = 0.0;
        indirect.specular = resolve.rgb;

        float3 worldPosition = GetWorldSpacePosition(i.uv);
        float3 worldViewDir = normalize(worldPosition - _WorldSpaceCameraPos);

        // resolve.rgb = UNITY_BRDF_PBS(gbuffer0.rgb, gbuffer1.rgb, oneMinusReflectivity, gbuffer1.a, normal, -worldViewDir, light, indirect).rgb;




#ifdef _ReflectionsNeedVL
        float4 vl = _RaytracedVolumetricLightingTexture.SampleLevel(sampler_RaytracedVolumetricLightingTexture, i.uv, 0.0);
        resolve = lerp(0, resolve, saturate(vl.r));
#endif

        // // uncomment to support deferred fog (unity does not support this by default) // // 
        // #if defined(_CORGI_FOG)
        //     #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2) 
        //          float3 clipPos = UnityObjectToClipPos(worldPosition);
        //          UNITY_APPLY_FOG(clipPos.z, resolve); 
        //     #endif
        // #endif

        FragmentOutput output;
        output.gbuffer0 = float4(resolve.rgb, gbuffer0.a);

        return output; 
    }

    ENDHLSL

        SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Blend One One

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag
                #pragma multi_compile _ _ReflectionsNeedVL
                // #pragma multi_compile _ _CORGI_FOG
                #pragma multi_compile_fog

            ENDHLSL
        }
    }
}