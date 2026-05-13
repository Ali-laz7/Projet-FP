#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceShadowsFeature : RaytraceFeatureBase
{
    [System.Serializable]
    public class RaytraceShadowsFeatureSettings : RaytraceFeatureSettings
    {
        [Header("Shadows-specific Settings")]
        [Range(0.001f, 0.5f)] public float shadowBias = 0.001f;
        [Range(0f, 1f)] public float shadowStrength = 1f;
        [Range(0.0001f, 0.01f)] public float shadowRaySeparation = 0.001f;
        public Color shadowColor = Color.black;

        [Header("Shadows-specific Quality")]
        public RaytraceQuality ShadowQuality;
        public bool smoothShadows = false;

        // public bool forwardRenderingGenerateNormals = false; // not in URP..? 
    }

    public RaytraceShadowsFeatureSettings settings = new RaytraceShadowsFeatureSettings();

    private RaytraceShadowsPass _renderPass;

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
        _renderPass = new RaytraceShadowsPass(settings);
    }

    public override RenderPassEvent WhenToInsert()
    {
        return RenderPassEvent.BeforeRenderingOpaques;
    }
}
#endif