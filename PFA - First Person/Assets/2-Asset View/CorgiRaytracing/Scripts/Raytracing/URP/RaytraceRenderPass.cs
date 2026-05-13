#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceRenderPass : RaytracePassBase
{
    public RaytraceRenderPass(RaytraceFeatureBase.RaytraceFeatureSettings settings) 
        : base(settings)
    {
        
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
    }

    protected override string GetEffectName()
    {
        return "RaytraceRender";
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceRenderPass";
    }

    protected override int GetRenderTextureMipMapCount()
    {
        return 1; 
    }

    protected override void ConfigureRaytraceCommands(CommandBuffer command)
    {
        base.ConfigureRaytraceCommands(command);
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "RaytraceRenderRayGeneration";
    }

    public override RenderTextureDescriptor GetRTDesc()
    {
        var desc = base.GetRTDesc();
        desc.colorFormat = RenderTextureFormat.ARGBHalf;

        return desc;
    }

    protected override void AppendCommandBufferAfterDispatch(CommandBuffer command)
    {
        base.AppendCommandBufferAfterDispatch(command);

#if UNITY_2022_1_OR_NEWER
        var cameraColorTargetHandle = _renderer.cameraColorTargetHandle;
        var cameraDepthTargetHandle = _renderer.cameraDepthTargetHandle;
#else
        var cameraColorTargetHandle = _renderer.cameraColorTarget;
        var cameraDepthTargetHandle = _renderer.cameraDepthTarget;
#endif

        // blit to get the rtx texture on top of the target 
        var renderData = settings.Data as URPRaytraceFeatureRenderData;
        command.SetGlobalTexture("_CopyBlitTex", _RenderTexture);
        command.SetRenderTarget(cameraColorTargetHandle, 0, CubemapFace.Unknown, -1);
        command.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, renderData.BlitToCameraMaterial, 0, 0);
    }
}
#endif