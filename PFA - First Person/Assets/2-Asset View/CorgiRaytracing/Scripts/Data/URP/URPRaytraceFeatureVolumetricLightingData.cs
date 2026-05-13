using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "URPRaytraceFeatureVolumetricLightingData", menuName = "CorgiRaytracing/Data/URPRaytraceFeatureVolumetricLightingData", order = 2)]
public class URPRaytraceFeatureVolumetricLightingData : URPRaytraceFeatureBaseData
{
    public ComputeShader BlurShader_SPI;
    public ComputeShader BlurShader_MP;
    public Material VLBlitMat;
}
