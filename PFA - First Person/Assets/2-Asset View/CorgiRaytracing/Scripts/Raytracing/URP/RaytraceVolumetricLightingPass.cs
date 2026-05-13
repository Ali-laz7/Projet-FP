#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceVolumetricLightingPass : RaytracePassBase
{
    public RaytraceVolumetricLightingPass(RaytraceFeatureBase.RaytraceFeatureSettings settings) 
        : base(settings)
    {
        
    }

    LightData _lightData;
    RenderingData _renderingData;

    public void CacheLightData(ref RenderingData renderingData)
    {
        _lightData = renderingData.lightData;
        _renderingData = renderingData;
    }

    public override RenderTextureDescriptor GetRTDesc()
    {
        var cameraRtDesc = base.GetRTDesc();
            cameraRtDesc.colorFormat = RenderTextureFormat.RFloat;

        return cameraRtDesc;
    }

    protected override FilterMode GetRenderTextureFilterMode()
    {
        return FilterMode.Bilinear;
    }

    protected override RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.RFloat;
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
    }

    protected override string GetEffectName()
    {
        return "RaytracedVolumetricLighting";
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceShadowsPass";
    }

    protected override int GetRenderTextureMipMapCount()
    {
        return 1;
    }


    protected override void ConfigureRaytraceCommands(CommandBuffer command)
    {
        base.ConfigureRaytraceCommands(command);

        var vlSettings = settings as RaytraceVolumetricLightingFeature.RaytraceVolumetricLightingFeatureSettings;
        var vlSettingsData = vlSettings.Data as URPRaytraceFeatureVolumetricLightingData;

        command.SetRayTracingFloatParam(vlSettingsData._RaytracingShader, "step_count", vlSettings.IterationSteps);
        command.SetRayTracingFloatParam(vlSettingsData._RaytracingShader, "step_intensity", vlSettings.IterationStrength * 10f);
        command.SetRayTracingFloatParam(vlSettingsData._RaytracingShader, "MaxShadowDistance", vlSettings.MaxShadowDistance);
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "URP_VolumetricLightingRayGen";
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

        var vlSettings = settings as RaytraceVolumetricLightingFeature.RaytraceVolumetricLightingFeatureSettings;
        var vlSettingsData = vlSettings.Data as URPRaytraceFeatureVolumetricLightingData;

        Blur(command);


        // urp only supports forward.. 
        var renderPath = RenderingPath.Forward; // camera.actualRenderingPath;
        var isDeferred = renderPath == RenderingPath.DeferredShading;

        var projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), false);
        var inverseProjection = projection.inverse;

        command.SetGlobalMatrix("_InverseProjection", inverseProjection);
        command.SetGlobalMatrix("_CameraToWorld", cameraData.camera.cameraToWorldMatrix);
        command.SetGlobalTexture("_RaytracedVolumetricLightingTexture", _RenderTexture);

        var grabpass = Shader.PropertyToID("_RaytracedGrabpass");

        var grabpassTargetDesc = cameraData.cameraTargetDescriptor;
            grabpassTargetDesc.enableRandomWrite = true;

        command.GetTemporaryRT(grabpass, grabpassTargetDesc);

        // manual blit, because URP + SPI does not allow Blits
        command.SetGlobalTexture("_CopyBlitTex", cameraColorTargetHandle);
        command.SetRenderTarget(grabpass, 0, CubemapFace.Unknown, -1);
        command.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, vlSettingsData.BlitMaterial, 0, 0);

        // command.Blit(BuiltinRenderTextureType.CurrentActive, grabpass);
        command.SetGlobalTexture("_CopyBlitTex", grabpass);

        command.SetRenderTarget(cameraColorTargetHandle, 0, CubemapFace.Unknown, -1);
        command.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, vlSettingsData.VLBlitMat, 0, 0);
    }


    private void Blur(CommandBuffer command)
    {
        var vlSettings = settings as RaytraceVolumetricLightingFeature.RaytraceVolumetricLightingFeatureSettings;
        var vlSettingsData = vlSettings.Data as URPRaytraceFeatureVolumetricLightingData;

        var kernelIndex = 2;

        var blurShader = vlSettingsData.BlurShader_MP;
        if(_RenderTexture.volumeDepth > 1)
        {
            blurShader = vlSettingsData.BlurShader_SPI;
        }

        // process the shadowmap via a compute shader 
        command.SetComputeFloatParam(blurShader, "_ShadowStrength", vlSettings.ShadowStrength);
        command.SetComputeIntParam(blurShader, "_textureWidth", _RenderTexture.width);
        command.SetComputeIntParam(blurShader, "_textureHeight", _RenderTexture.height);

        var _VLTemp = Shader.PropertyToID("_VLTemp");
        command.GetTemporaryRT(_VLTemp, GetRTDesc()); 
        command.CopyTexture(_RenderTexture, _VLTemp);

        command.SetComputeTextureParam(blurShader, kernelIndex, "_ShadowmapTemp", _VLTemp);

        command.SetComputeTextureParam(blurShader, kernelIndex, "_ShadowMap", _RenderTexture);
        command.DispatchCompute(blurShader, kernelIndex, _RenderTexture.width / 32, _RenderTexture.height / 32, _RenderTexture.volumeDepth);
    }

}
#endif