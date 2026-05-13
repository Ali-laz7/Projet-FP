#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceVolumetricLightingFeature : RaytraceFeatureBase
{
    [System.Serializable]
    public class RaytraceVolumetricLightingFeatureSettings : RaytraceFeatureSettings
    {
        [Header("VL-specific Settings")]
        [Range(0f, 1f)] public float ShadowStrength = 1f;
        [Range(32, 512)] public int IterationSteps = 64;
        [Range(10f, 1000f)] public float MaxShadowDistance = 100f;
        [Range(0f, 10f)] public float IterationStrength = 1f;
    }

    public RaytraceVolumetricLightingFeatureSettings settings = new RaytraceVolumetricLightingFeatureSettings();

    private RaytraceVolumetricLightingPass _renderPass;

    protected override RaytraceFeatureBase.RaytraceFeatureSettings GetSettings()
    {
        return settings;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (CheckDisableIfMSAA(ref renderingData))
        {
            return;
        }

        _renderPass.Setup(renderingData.cameraData, renderer, WhenToInsert());
        _renderPass.CacheLightData(ref renderingData);

        renderer.EnqueuePass(_renderPass);
    }

    public override void Create()
    {
        _renderPass = new RaytraceVolumetricLightingPass(settings);
    }

    public override RenderPassEvent WhenToInsert()
    {
        return RenderPassEvent.AfterRenderingSkybox;
    }
}
#endif