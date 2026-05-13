using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// [CreateAssetMenu( fileName = "URPRaytraceFeatureBaseData", menuName = "CorgiRaytracing/Data/URPRaytraceFeatureBaseData", order = 0)]
public class URPRaytraceFeatureBaseData : ScriptableObject
{
    public RayTracingShader _RaytracingShader;
    public Material BlitMaterial;

    // public ComputeShader TemporalReprojection;

    // NOTE: URP does not support motion vectors yet, so no reprojection available yet!
    // [Header("Temporal Reprojection")]
    // public bool TemporallyRenderEffect;
    // public bool TemporallyReproject;
    // [Range(1, 8)] public int TemporalEffectFrameDuration = 4;
}
