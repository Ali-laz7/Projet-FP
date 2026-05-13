#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytracePassBase : ScriptableRenderPass
{
    private string _profilerTag;
    // private RenderTargetHandle _tempTexture;

    protected RaytraceFeatureBase.RaytraceFeatureSettings settings;
    protected CameraData cameraData;
    protected ScriptableRenderer _renderer;

    public RaytracePassBase(RaytraceFeatureBase.RaytraceFeatureSettings settings)
    {
        this.renderPassEvent = renderPassEvent;

        _profilerTag = GetEffectName();
        this.settings = settings;
    }

    public void Setup(CameraData cameraData, ScriptableRenderer renderer, RenderPassEvent whenToInsert)
    {
        this.cameraData = cameraData;
        this._renderer = renderer;

        renderPassEvent = whenToInsert;
    }

    // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    // {
    //     cmd.GetTemporaryRT(_tempTexture.id, cameraTextureDescriptor);
    // }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!RaytraceDataManager.IsReady())
        {
            return;
        }

        var cmd = CommandBufferPool.Get(_profilerTag);

        BuildCommandBuffer(cmd);

        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd); 
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        // cmd.ReleaseTemporaryRT(_tempTexture.id); 
    }

    // 
    //  Raytrace stuff 
    // 
    public RenderTexture _RenderTexture;

    // protected int _renderTextureWidth;
    // protected int _renderTextureHeight;
    // protected int _renderTextureDepth;

    // public virtual RenderTargetIdentifier GetRenderTextureHandle()
    // {
    //     return new RenderTargetIdentifier(GetRenderTextureId());
    // }
    // 
    // public virtual int GetRenderTextureId()
    // {
    //     return Shader.PropertyToID("_RTXReflectionsTex");
    // }

    protected virtual void ConfigureRaytraceCommandsMatrices(CommandBuffer command)
    {
        var camera = cameraData.camera;

        var projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), false);
        var inverseProjection = projection.inverse;

        var viewProjectionMatrix = cameraData.GetViewMatrix() * cameraData.GetProjectionMatrix();

        command.SetRayTracingMatrixParam(settings.Data._RaytracingShader, "_InverseProjection", inverseProjection);
        command.SetRayTracingMatrixParam(settings.Data._RaytracingShader, "UNITY_MATRIX_VP", viewProjectionMatrix);
        command.SetRayTracingMatrixParam(settings.Data._RaytracingShader, "_CameraToWorld", camera.cameraToWorldMatrix);
        command.SetRayTracingVectorParam(settings.Data._RaytracingShader, "_WorldSpaceCameraPos", camera.transform.position);

        // single pass instanced rendering stuff.. 
        var eyeCount = cameraData.cameraTargetDescriptor.volumeDepth;
        var _InverseProjectionArray = new Matrix4x4[2];
        var UNITY_MATRIX_VPArray = new Matrix4x4[2];

        _InverseProjectionArray[0] = inverseProjection;
        UNITY_MATRIX_VPArray[0] = viewProjectionMatrix;

        for (var eyeIndex = 1; eyeIndex < Mathf.Min(2, eyeCount); ++eyeIndex)
        {
            var eye_projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(eyeIndex), false);
            var eye_inverseProjection = eye_projection.inverse;

            var eye_viewProjectionMatrix = cameraData.GetViewMatrix(eyeIndex) * cameraData.GetProjectionMatrix(eyeIndex);

            _InverseProjectionArray[eyeIndex] = eye_inverseProjection;
            UNITY_MATRIX_VPArray[eyeIndex] = eye_viewProjectionMatrix;
        }

        command.SetRayTracingMatrixArrayParam(settings.Data._RaytracingShader, "_Array_InverseProjection", _InverseProjectionArray);
        command.SetRayTracingMatrixArrayParam(settings.Data._RaytracingShader, "Array_UNITY_MATRIX_VP", UNITY_MATRIX_VPArray);

        // NOTE: URP does not yet support motion vectors 
        // if (settings.TemporallyRenderEffect)
        // {
        //     _temporal_pass_index++;
        //     if (_temporal_pass_index > settings.TemporalEffectFrameDuration)
        //     {
        //         _temporal_pass_index = 0;
        //     }
        // 
        //     command.SetRayTracingIntParam(settings._RaytracingShader, "TemporalPassIndex", _temporal_pass_index);
        //     command.SetRayTracingIntParam(settings._RaytracingShader, "TemporalPassCount", settings.TemporalEffectFrameDuration);
        // }
    }

    protected virtual void ConfigureRaytraceCommands(CommandBuffer command)
    {
        GetRaytracingAccelerationStructureData(out RayTracingAccelerationStructure structure, out string structureName);

        command.SetRayTracingAccelerationStructure(settings.Data._RaytracingShader, structureName, structure);
        command.SetGlobalInt("_RaytraceAgainstLayers", RaytraceDataManager.Instance.UpdateLayers.value);
        command.SetGlobalInt("_RayFlags", (int)settings._RayFlags);
        command.SetGlobalFloat("_MaxRayDistance", settings.MaxRayDistance);
        command.SetRayTracingShaderPass(settings.Data._RaytracingShader, GetRaytracingShaderPassName());

        ConfigureRaytraceCommandsMatrices(command);

        command.SetRayTracingTextureParam(settings.Data._RaytracingShader, "RenderTarget", _RenderTexture);

        // NOTE: URP does not yet support motion vectors 
        // command.SetRayTracingIntParam(settings._RaytracingShader, "TemporallyRendered", settings.TemporallyRenderEffect ? 1 : 0);
        command.SetRayTracingIntParam(settings.Data._RaytracingShader, "TemporallyRendered", 0);
    }

    protected virtual void DispatchRays(CommandBuffer command, RenderingPath renderPath)
    {
        command.DispatchRays(settings.Data._RaytracingShader, GetRaygenShaderName(renderPath), (uint)_RenderTexture.width, (uint)_RenderTexture.height, (uint)_RenderTexture.volumeDepth);
    }

    protected virtual void BuildCommandBuffer(CommandBuffer command)
    {
        command.Clear();

        EnsureRT(command);

        // NOTE: URP does not yet support motion vectors 
        // if(settings.TemporallyRenderEffect && settings.TemporallyReproject)
        // {
        //     BuildTemporalReprojection(command); 
        // }

        ConfigureRaytraceCommands(command);
        DispatchRays(command, RenderingPath.Forward); // todo 
        AppendCommandBufferAfterDispatch(command);
    }

    protected virtual void GetRaytracingAccelerationStructureData(out RayTracingAccelerationStructure structure, out string structureName)
    {
        structure = RaytraceDataManager.Instance._AccelerationStructure;
        structureName = "_RaytracingAccelerationStructure";
    }

    protected virtual string GetRaytracingShaderPassName()
    {
        return "RaytracePass";
    }

    protected virtual string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "MyRaygenShader";
    }

    protected virtual string GetEffectName()
    {
        return "RaytraceEffectBase";
    }

    protected int _temporal_pass_index = 0;
    protected bool previous_SetOnce;
    protected Matrix4x4 previous_CameraToWorld;
    protected Matrix4x4 previous_InverseProjection;

    protected void BuildTemporalReprojection(CommandBuffer command)
    {
        // note: URP does not generate motion vectors; so temporal reprojection is not currently possible on CorgiRaytracing effects in URP 
        // temporal reprojection
        // if (settings.TemporallyRenderEffect && settings.TemporallyReproject && settings.TemporalReprojection != null)
        // {
        //     command.Clear();
        // 
        //     // note: would be cool to get rid of this blit 
        //     var _Temporal = Shader.PropertyToID("_Temporal" + GetEffectName());
        //     command.GetTemporaryRT(_Temporal, _renderTargetDesc);
        // 
        //     command.CopyTexture(_RenderTexture, 0, 0, _Temporal, 0, 0);
        // 
        //     var kernal_reprojection = 0;
        //     command.SetComputeTextureParam(settings.TemporalReprojection, kernal_reprojection, "Input", _Temporal);
        //     command.SetComputeTextureParam(settings.TemporalReprojection, kernal_reprojection, "Output", _RenderTexture);
        // 
        //     command.SetComputeIntParam(settings.TemporalReprojection, "texture_width", _renderTargetDesc.width);
        //     command.SetComputeIntParam(settings.TemporalReprojection, "texture_height", _renderTargetDesc.height);
        // 
        //     command.SetComputeIntParam(settings.TemporalReprojection, "TemporalPassIndex", _temporal_pass_index);
        //     command.SetComputeIntParam(settings.TemporalReprojection, "TemporalPassCount", settings.TemporalEffectFrameDuration);
        // 
        //     // no motion vectors in URP..? 
        //     var textureId = BuiltinRenderTextureType.MotionVectors;
        //     command.SetComputeTextureParam(settings.TemporalReprojection, kernal_reprojection, "_CameraMotionVectorsTexture", textureId);
        // 
        //     command.SetComputeVectorParam(settings.TemporalReprojection, "_CameraMotionVectorsTexture_Resolution", new Vector4(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight));
        // 
        //     command.DispatchCompute(settings.TemporalReprojection, kernal_reprojection, _renderTargetDesc.width / 32, _renderTargetDesc.height / 32, 1);
        // }
    }

    protected virtual void AppendCommandBufferAfterDispatch(CommandBuffer command)
    {

    }

    public virtual RenderTextureDescriptor GetRTDesc()
    {
        var rtDesc = cameraData.cameraTargetDescriptor;

        var res_x = rtDesc.width / settings.TextureScaleReciprocal;
        var res_y = rtDesc.height / settings.TextureScaleReciprocal;

        // force power of 2 
        var resolution = Mathf.Max(res_x, res_y);
        resolution = Mathf.ClosestPowerOfTwo(resolution);

        res_x = resolution;
        res_y = resolution;

        // var mipCount = GetRenderTextureMipMapCount();

        rtDesc.width = res_x;
        rtDesc.height = res_y;
        rtDesc.enableRandomWrite = true;

        // cant allow in rtx effects..? 
        rtDesc.msaaSamples = 1;

        return rtDesc;
    }

    protected RenderTextureDescriptor _renderTargetDesc;

    protected void EnsureRT(CommandBuffer command)
    {
        _renderTargetDesc = GetRTDesc();
        // command.GetTemporaryRT(GetRenderTextureId(), _renderTargetDesc); 

        if (_RenderTexture != null && _renderTargetDesc.width == _RenderTexture.width && _renderTargetDesc.height == _RenderTexture.height && _renderTargetDesc.volumeDepth == _RenderTexture.volumeDepth)
        {
            return;
        }

        if(_RenderTexture != null)
        {
            _RenderTexture.Release();
            _RenderTexture = null; 
        }

         // Debug.Log($"Generating {_renderTargetDesc.width},{_renderTargetDesc.height},{_renderTargetDesc.volumeDepth} sized texture. " +
         //     $"dimension: {cameraData.cameraTargetDescriptor.dimension}, " +
         //     $"vrUsage: {cameraData.cameraTargetDescriptor.vrUsage}");

         // 
         //     if (_RenderTexture != null)
         //     {
         //         _RenderTexture.Release();
         //     }
         // 
         //     // _RenderTexture = new RenderTexture(res_x, res_y, 24, GetRenderTextureFormat(), mipCount);

        _RenderTexture = new RenderTexture(_renderTargetDesc);
        _RenderTexture.name = $"_{GetEffectName()}Tex";
        _RenderTexture.autoGenerateMips = false;
        _RenderTexture.enableRandomWrite = true;
        
        _RenderTexture.filterMode = GetRenderTextureFilterMode();
        
        _RenderTexture.anisoLevel = 1;
        _RenderTexture.antiAliasing = 1;

        // _RenderTexture.anisoLevel = settings.AnisoLevelSetting;
        // _RenderTexture.antiAliasing = settings.AntiAliasSetting;

        _RenderTexture.Create(); 
    }

    protected virtual RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.ARGB32;
    }

    protected virtual int GetRenderTextureMipMapCount()
    {
        return 1;
    }

    protected virtual FilterMode GetRenderTextureFilterMode()
    {
        return FilterMode.Bilinear;
    }
}
#endif