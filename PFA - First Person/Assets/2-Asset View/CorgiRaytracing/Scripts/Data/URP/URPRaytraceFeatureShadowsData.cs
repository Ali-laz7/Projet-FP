using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "URPRaytraceFeatureShadowsData", menuName = "CorgiRaytracing/Data/URPRaytraceFeatureShadowsData", order = 1)]
public class URPRaytraceFeatureShadowsData : URPRaytraceFeatureBaseData
{
    // no deferred in URP, yet 
    // public Shader DeferredShadingOverride;

    public ComputeShader ForwardShadows_SPI;
    public ComputeShader ForwardShadows_MP;
}
