#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceShadowsPass : RaytracePassBase
{
    public RaytraceShadowsPass(RaytraceFeatureBase.RaytraceFeatureSettings settings) 
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
        return "RaytraceShadows";
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceShadowsPass";
    }

    protected override int GetRenderTextureMipMapCount()
    {
        return 1;
    }


    // protected override void ConfigureRaytraceCommandsMatrices(CommandBuffer command)
    // {
    //     VisibleLight shadowLight = _lightData.visibleLights[_lightData.mainLightIndex];
    //     Light light = shadowLight.light;
    // 
    //     Bounds bounds;
    //     if (!_renderingData.cullResults.GetShadowCasterBounds(_lightData.mainLightIndex, out bounds))
    //     {
    //         Debug.LogError("out of bounds?");
    //         return;
    //     }
    // 
    //     ShadowData shadowData = _renderingData.shadowData;
    //     shadowData.mainLightShadowCascadesCount = 1;
    //     shadowData.mainLightShadowCascadesSplit = new Vector3();
    //     shadowData.mainLightShadowmapWidth = _RenderTexture.width;
    //     shadowData.mainLightShadowmapHeight = _RenderTexture.height;
    //     shadowData.supportsMainLightShadows = true;
    //     shadowData.bias = new List<Vector4>();
    // 
    //     bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref _renderingData.cullResults, ref shadowData,
    //                 _lightData.mainLightIndex, 0, _RenderTexture.width, _RenderTexture.height, _RenderTexture.width, light.shadowNearPlane,
    //                 out Vector4 cascadeSplitDifferences, out ShadowSliceData cascadeSlices, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix);
    // 
    //     if(!success)
    //     {
    //         Debug.LogError("fail?");
    //         return;
    //     }
    // 
    //     var shadowmapMatrix = projectionMatrix * viewMatrix;
    //     command.SetGlobalMatrix("_MainLightInverseProjection", projectionMatrix.inverse);
    //     command.SetGlobalMatrix("_MainLightLocalToWorld", viewMatrix.inverse);
    //     command.SetRayTracingVectorParam(settings.Data._RaytracingShader, "_MainLightDirection", shadowLight.light.transform.forward);
    // }

    protected override void ConfigureRaytraceCommands(CommandBuffer command)
    {
        base.ConfigureRaytraceCommands(command);

        var shadowSettings = settings as RaytraceShadowsFeature.RaytraceShadowsFeatureSettings;
        var shadowSettingsData = shadowSettings.Data as URPRaytraceFeatureShadowsData;

        command.SetGlobalFloat("_ShadowBias", shadowSettings.shadowBias);

        switch (shadowSettings.ShadowQuality)
        {
            case RaytraceQuality.Low:
                command.SetGlobalInt("ShadowCastCount", 1);
                break;
            case RaytraceQuality.Med:
                command.SetGlobalInt("ShadowCastCount", 8);
                break;
            case RaytraceQuality.High:
                command.SetGlobalInt("ShadowCastCount", 16);
                break;
            case RaytraceQuality.Overkill:
                command.SetGlobalInt("ShadowCastCount", 32);
                break;
        }

        command.SetGlobalFloat("_RaySeparation", shadowSettings.shadowRaySeparation);

        var renderPath = RenderingPath.Forward; // URP only supports forward.. 
        if (renderPath == RenderingPath.Forward)
        {
            // if (forwardRenderingGenerateNormals)
            // {
            //     command.EnableShaderKeyword("_HAS_DEPTH_NORMALS");
            // }
            // else
            // {
                command.DisableShaderKeyword("_HAS_DEPTH_NORMALS");
            // }
        }
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "URP_ShadowRayGeneration";
    }

    protected override void AppendCommandBufferAfterDispatch(CommandBuffer command)
    {
        base.AppendCommandBufferAfterDispatch(command);

        // process the shadowmap via a compute shader 
        var shadowSettings = settings as RaytraceShadowsFeature.RaytraceShadowsFeatureSettings;
        var shadowSettingsData = shadowSettings.Data as URPRaytraceFeatureShadowsData;

        var smoothComputeShader = shadowSettingsData.ForwardShadows_MP;
        if(_RenderTexture.volumeDepth > 1)
        {
            smoothComputeShader = shadowSettingsData.ForwardShadows_SPI;
        }


        command.SetComputeFloatParam(smoothComputeShader, "_ShadowStrength", shadowSettings.shadowStrength);
        command.SetComputeIntParam(smoothComputeShader, "_textureWidth", _RenderTexture.width);
        command.SetComputeIntParam(smoothComputeShader, "_textureHeight", _RenderTexture.height);

        // note: would be cool to get rid of this blit 
        var _CorgiShadowmap = Shader.PropertyToID("_CorgiShadowmap");

        command.GetTemporaryRT(_CorgiShadowmap, GetRTDesc());
        command.CopyTexture(_RenderTexture, _CorgiShadowmap);
        
        if (shadowSettings.smoothShadows)
        {
            command.SetComputeTextureParam(smoothComputeShader, 0, "_ShadowmapTemp", _RenderTexture);
            command.SetComputeTextureParam(smoothComputeShader, 0, "_ShadowMap", _CorgiShadowmap);
            command.DispatchCompute(smoothComputeShader, 0, _RenderTexture.width / 32, _RenderTexture.height / 32, _RenderTexture.volumeDepth);
        }
        else
        {
            command.SetComputeTextureParam(smoothComputeShader, 1, "_ShadowmapTemp", _RenderTexture);
            command.SetComputeTextureParam(smoothComputeShader, 1, "_ShadowMap", _CorgiShadowmap);
            command.DispatchCompute(smoothComputeShader, 1, _RenderTexture.width / 32, _RenderTexture.height / 32, _RenderTexture.volumeDepth);
        }

        // set some unity specific variables, to hack in our rtx-based shadowmap
        // command.SetGlobalTexture("_ShadowMapTexture", _CorgiShadowmap); //builtin 
        command.SetGlobalTexture("_RaytracingShadowMapTexture", _CorgiShadowmap);

        // URP
        command.SetGlobalTexture("_ScreenSpaceOcclusionTexture", _CorgiShadowmap); 
        command.EnableShaderKeyword("_SCREEN_SPACE_OCCLUSION");

    }
}
#endif