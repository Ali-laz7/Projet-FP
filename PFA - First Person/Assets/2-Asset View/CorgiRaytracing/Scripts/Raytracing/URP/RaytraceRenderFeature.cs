#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceRenderFeature : RaytraceFeatureBase
{

    [System.Serializable]
    public class RaytraceRenderFeatureSettings : RaytraceFeatureSettings
    {

    }

    public RaytraceRenderFeatureSettings settings = new RaytraceRenderFeatureSettings();

    private RaytraceRenderPass _renderPass;

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
        _renderPass = new RaytraceRenderPass(settings);
    }

    public override RenderPassEvent WhenToInsert()
    {
        return RenderPassEvent.AfterRenderingSkybox + 0;
    }
}
#endif