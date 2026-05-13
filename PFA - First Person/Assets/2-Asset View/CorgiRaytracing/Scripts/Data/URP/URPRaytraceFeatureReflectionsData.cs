using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "URPRaytraceFeatureReflectionsData", menuName = "CorgiRaytracing/Data/URPRaytraceFeatureReflectionsData", order = 0)]
public class URPRaytraceFeatureReflectionsData : URPRaytraceFeatureBaseData
{
    // no deferred in URP, yet 
    // public Shader DeferredReflections;

    public Shader ForwardReflections;
    public Texture FallbackSkybox;

    public Material BlitToCameraMaterial;
}
