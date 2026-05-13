#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceReflectionsPass : RaytracePassBase
{
    public RaytraceReflectionsPass(RaytraceFeatureBase.RaytraceFeatureSettings settings) 
        : base(settings)
    {
        
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
    }

    protected override string GetEffectName()
    {
        return "RaytraceReflections";
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceReflectionPass";
    }

    protected override int GetRenderTextureMipMapCount()
    {
        var renderPath = RenderingPath.Forward; // note: URP only supports forward right now.. 
        var reflectionSettings = settings as RaytraceReflectionsFeature.RaytraceReflectionsFeatureSettings;
        var useMips = reflectionSettings.useBlurredMips && renderPath == RenderingPath.DeferredShading;
        return useMips ? 6 : 1;
    }

    protected override void ConfigureRaytraceCommands(CommandBuffer command)
    {
        base.ConfigureRaytraceCommands(command);

        var camera = cameraData.camera;
        var bounce_count_offset = 0f; //  context.renderPath == RenderingPath.DeferredShading ? 0f : -1f;

        var reflectionSettings = settings as RaytraceReflectionsFeature.RaytraceReflectionsFeatureSettings;
        switch (reflectionSettings.Bounces)
        {
            default:
            case RaytraceQuality.Low:
                float low_count = 2f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", low_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (low_count + 1));
                break;
            case RaytraceQuality.Med:
                float med_count = 3f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", med_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (med_count + 1));
                break;
            case RaytraceQuality.High:
                float high_count = 4f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", high_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (high_count + 1));
                break;
            case RaytraceQuality.Overkill:
                float overkill_count = 5f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", overkill_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (overkill_count + 1));
                break;
        }

        switch (reflectionSettings.Roughness)
        {
            default:
            case RaytraceQuality.Low:
                command.SetGlobalFloat("MAX_ROUGHNESS_COUNT", 4);
                break;
            case RaytraceQuality.Med:
                command.SetGlobalFloat("MAX_ROUGHNESS_COUNT", 8);
                break;
            case RaytraceQuality.High:
                command.SetGlobalFloat("MAX_ROUGHNESS_COUNT", 16);
                break;
            case RaytraceQuality.Overkill:
                command.SetGlobalFloat("MAX_ROUGHNESS_COUNT", 32);
                break;
        }

        switch (reflectionSettings.ShadowsInReflections)
        {
            default:
            case RaytraceQuality.Low:
                command.SetGlobalFloat("CAST_SHADOW_BOUNCE_CAP", 1);
                break;
            case RaytraceQuality.Med:
                command.SetGlobalFloat("CAST_SHADOW_BOUNCE_CAP", 3);
                break;
            case RaytraceQuality.High:
                command.SetGlobalFloat("CAST_SHADOW_BOUNCE_CAP", 6);
                break;
            case RaytraceQuality.Overkill:
                command.SetGlobalFloat("CAST_SHADOW_BOUNCE_CAP", 8);
                break;
        }

        if (reflectionSettings.skybox == null)
        {
            var reflectionsData = reflectionSettings.Data as URPRaytraceFeatureReflectionsData;
            reflectionSettings.skybox = reflectionsData.FallbackSkybox;
        }

        command.SetRayTracingTextureParam(settings.Data._RaytracingShader, "SkyboxTex", reflectionSettings.skybox);

        if (reflectionSettings.ReflectionsHaveFog || camera.renderingPath == RenderingPath.Forward)
        {
            command.EnableShaderKeyword("_CORGI_FOG");
        }
        else
        {
            command.DisableShaderKeyword("_CORGI_FOG");
        }
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        if (renderPath == RenderingPath.DeferredShading)
        {
            return "ReflectionRayGeneration_Deferred";
        }
        else
        {
            return "ReflectionRayGeneration_Forward";
        }
    }

    protected override void AppendCommandBufferAfterDispatch(CommandBuffer command)
    {
        var reflectionSettings = settings as RaytraceReflectionsFeature.RaytraceReflectionsFeatureSettings;

        base.AppendCommandBufferAfterDispatch(command);

#if UNITY_2022_1_OR_NEWER
        var cameraColorTargetHandle = _renderer.cameraColorTargetHandle;
        var cameraDepthTargetHandle = _renderer.cameraDepthTargetHandle;
#else
        var cameraColorTargetHandle = _renderer.cameraColorTarget;
        var cameraDepthTargetHandle = _renderer.cameraDepthTarget;
#endif

        var camera = cameraData.camera;

        var projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), false);
        var inverseProjection = projection.inverse;

        var renderPath = RenderingPath.Forward; // note: URP only supports forward right now.. 
        if (reflectionSettings.useBlurredMips && renderPath == RenderingPath.DeferredShading)
        {
            GenerateLODs(command);
        }

        command.SetGlobalMatrix("corgi_InverseProjection", inverseProjection);
        command.SetGlobalMatrix("corgi_CameraToWorld", camera.cameraToWorldMatrix);
        command.SetGlobalTexture("_RTXReflectionsTex", _RenderTexture);

        var grabpassTargetDesc = cameraData.cameraTargetDescriptor;
            grabpassTargetDesc.enableRandomWrite = true;

        // NOTE: URP does not yet support motion vectors 
        // if (settings.TemporallyRenderEffect && settings.TemporalReprojection)
        // {
        //     var reflectionsGrabpass = Shader.PropertyToID("_ReflectionsGrabpass");
        // 
        //     command.GetTemporaryRT(reflectionsGrabpass, grabpassTargetDesc);
        // 
        //     // manual blit, because URP + SPI does not allow Blits
        //     command.SetGlobalTexture("_CopyBlitTex", _cameraColorTarget);
        //     command.SetRenderTarget(reflectionsGrabpass, 0, CubemapFace.Unknown, -1);
        //     command.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, reflectionSettings.BlitMaterial, 0, 0);
        // 
        //     command.SetGlobalTexture("_ReflectionsGrabpass", reflectionsGrabpass);
        //     command.SetRenderTarget(_cameraColorTarget, 0, CubemapFace.Unknown, -1);
        // }

        // NOTE: URP does not yet support deferred 
        // else if (renderPath == RenderingPath.DeferredShading)
        // {
        // 
        // }

        // else
        {
            var reflectionsGrabpass = Shader.PropertyToID("_ReflectionsGrabpass");
            command.GetTemporaryRT(reflectionsGrabpass, grabpassTargetDesc);

            // manual blit, because URP + SPI does not allow Blits
            command.SetGlobalTexture("_CopyBlitTex", cameraColorTargetHandle);
            command.SetRenderTarget(reflectionsGrabpass, 0, CubemapFace.Unknown, -1); 
            command.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, reflectionSettings.Data.BlitMaterial, 0, 0);

            command.SetGlobalTexture("_ReflectionsGrabpass", reflectionsGrabpass);
            command.SetRenderTarget(cameraColorTargetHandle, 0, CubemapFace.Unknown, -1);
        }


        // it would be cool if we could do something like this? just set a texture and then let forward rendering take care of it.. 
        // command.SetGlobalTexture("_RaytracedReflections", _RenderTexture);

        // var desc = new RenderTextureDescriptor(_RenderTexture.width, _RenderTexture.height, _RenderTexture.format, _RenderTexture.depth, _RenderTexture.mipmapCount);
        //     desc.dimension = TextureDimension.Cube;
        // 
        // var _ReflectionCube = Shader.PropertyToID("_ReflectionCube");
        // 
        // command.GetTemporaryRT(_ReflectionCube, desc); 
        // command.Blit(_RenderTexture, _ReflectionCube);
        // 
        // command.SetGlobalTexture("unity_SpecCube0", _ReflectionCube);

        // var existingVL = GetComponent<RaytraceEffectVolumetricLighting>();
        // if (TemporallyRenderEffect && TemporallyReproject && existingVL != null && existingVL.enabled)
        // {
        //     command.EnableShaderKeyword("_ReflectionsNeedVL");
        // }
        // else
        // {
        //     command.DisableShaderKeyword("_ReflectionsNeedVL");
        // }

        var reflectionsData = reflectionSettings.Data as URPRaytraceFeatureReflectionsData;
        command.DrawMesh(RaytraceDataManager.fullscreenTriangle, Matrix4x4.identity, reflectionsData.BlitToCameraMaterial, 0, 0);
    }

    // todo, our own gaussian solution 
    private int[] m_MipIDs;
    public ComputeShader gaussianDownsample;

    public void GenerateLODs(CommandBuffer command)
    {
        if (gaussianDownsample == null)
        {
            return;
        }

        int kMaxLods = 6;
        if (m_MipIDs == null || m_MipIDs.Length != _renderTargetDesc.mipCount)
        {
            m_MipIDs = new int[kMaxLods];
        
            for (int i = 0; i < kMaxLods; i++)
                m_MipIDs[i] = Shader.PropertyToID("_GaussianMip" + i);
        }
        
        var compute = gaussianDownsample;
        int kernel = compute.FindKernel("KMain");
        var mipFormat = _renderTargetDesc.colorFormat;
        
        var last = new RenderTargetIdentifier(_RenderTexture, 0);
        
        int lodCount = kMaxLods - 1;
        
        int width = _renderTargetDesc.width;
        int height = _renderTargetDesc.height;
        
        // Mathf.ClosestPowerOfTwo(Mathf.Min(context.width, context.height));
        
        for (int i = 0; i < lodCount; i++)
        {
            width /= 2;
            height /= 2;
        
            command.GetTemporaryRT(m_MipIDs[i], width, height, 0, FilterMode.Bilinear, mipFormat, RenderTextureReadWrite.Default, 1, true);
            command.SetComputeTextureParam(compute, kernel, "_Source", last);
            command.SetComputeTextureParam(compute, kernel, "_Result", m_MipIDs[i]);
            command.SetComputeVectorParam(compute, "_Size", new Vector4(width, height, 1f / width, 1f / height));
            command.DispatchCompute(compute, kernel, width / 8, height / 8, 1);
            command.CopyTexture(m_MipIDs[i], 0, 0, _RenderTexture, 0, i + 1);
        
            last = m_MipIDs[i];
        }
        
        for (int i = 0; i < lodCount; i++)
        {
            command.ReleaseTemporaryRT(m_MipIDs[i]);
        }
    }
}
#endif