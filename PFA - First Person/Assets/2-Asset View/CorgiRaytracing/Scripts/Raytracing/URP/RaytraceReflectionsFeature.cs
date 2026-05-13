#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceReflectionsFeature : RaytraceFeatureBase
{
    [System.Serializable]
    public class RaytraceReflectionsFeatureSettings : RaytraceFeatureSettings
    {
        [Header("Reflection-Specific Settings")]
        [Tooltip("set this to be your real scene's skybox texture, or an approximation of your skybox material")] public Texture skybox;
        [Tooltip("if enabled, a blur pass will be done for rough reflections")] public bool useBlurredMips;
        [Tooltip("if enabled, the raytrace shader will try to use your real fog settings when calculating the final color for rays")] public bool ReflectionsHaveFog;

        [Header("Reflection-Specific Quality Settings")]
        [Tooltip("a higher value will result in a brighter and more accurate image, but may potentially double the performance cost with each quality increase in the worst case scenario")] public RaytraceQuality Bounces;
        [Tooltip("a higher value will allow for bumpier surfaces to be rougher without a blur pass, however performance will be seriously impacted as more rays will be cast from rough surfaces")] public RaytraceQuality Roughness;
        [Tooltip("a higher value will allow shadows to be estimated another bounce deeper in a ray's lifetime.")] public RaytraceQuality ShadowsInReflections;
    }

    public RaytraceReflectionsFeatureSettings settings = new RaytraceReflectionsFeatureSettings();

    private RaytraceReflectionsPass _renderPass;

    protected override RaytraceFeatureBase.RaytraceFeatureSettings GetSettings()
    {
        return settings;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(CheckDisableIfMSAA(ref renderingData))
        {
            return;
        }

        _renderPass.Setup(renderingData.cameraData, renderer, WhenToInsert());

        renderer.EnqueuePass(_renderPass);
    }

    public override void Create()
    {
        _renderPass = new RaytraceReflectionsPass(settings);
    }

    public override RenderPassEvent WhenToInsert()
    {
        // +1 after RaytraceRenderFEature 
        return RenderPassEvent.AfterRenderingSkybox + 1;
    }
}
#endif