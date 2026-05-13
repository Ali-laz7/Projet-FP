using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceEffectVolumetricLighting))]
public class RaytraceEffectVolumetricLightingEditor : RaytraceEffectBaseEditor
{
    protected SerializedProperty ShadowStrength;
    protected SerializedProperty IterationSteps;
    protected SerializedProperty IterationStrength;
    protected SerializedProperty BlurShader;
    protected SerializedProperty BlitShader;
    protected SerializedProperty MaxShadowDistance;

    protected override void OnEnable()
    {
        base.OnEnable();

        ShadowStrength = serializedObject.FindProperty("ShadowStrength");
        IterationSteps = serializedObject.FindProperty("IterationSteps");
        IterationStrength = serializedObject.FindProperty("IterationStrength");
        BlurShader = serializedObject.FindProperty("BlurShader");
        BlitShader = serializedObject.FindProperty("BlitShader");
        MaxShadowDistance = serializedObject.FindProperty("MaxShadowDistance");
    }

    private bool settingsFoldout;
    private bool dataFoldout;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (WarnUseError())
        {
            return;
        }

        EditorGUILayout.BeginVertical("GroupBox");
        settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings", true);
        
        if (settingsFoldout)
        {
            EditorGUILayout.PropertyField(ShadowStrength);
            EditorGUILayout.PropertyField(IterationSteps);
            EditorGUILayout.PropertyField(IterationStrength);
            EditorGUILayout.PropertyField(MaxShadowDistance);
            DrawSharedSettings();

            // we don't yet support this for VL 
            // DrawReprojectionSettings(); 
        }

        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("GroupBox");
        
        dataFoldout = EditorGUILayout.Foldout(dataFoldout, "Data", true);
        
        if (dataFoldout)
        {
            EditorGUILayout.PropertyField(_RaytracingShader);
            EditorGUILayout.PropertyField(BlurShader);
            EditorGUILayout.PropertyField(BlitShader);
            EditorGUILayout.PropertyField(TemporalReprojection);
        }
        
        EditorGUILayout.EndVertical();
        
        DrawRenderTexture();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif

public class RaytraceEffectVolumetricLighting : RaytraceEffectBase
{
    // settings 
    [Range(0f, 1f)] public float ShadowStrength = 1f; 
    [Range(32, 512)] public int IterationSteps = 64; 
    [Range(10f, 1000f)] public float MaxShadowDistance = 100f; 
    [Range(0f, 10f)] public float IterationStrength = 1f; 

    // data 
    public ComputeShader BlurShader;
    public Shader BlitShader;

    private Material _blitMaterial;

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_blitMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_blitMaterial);
            }
            else
            {
                DestroyImmediate(_blitMaterial);
            }
        }
    }

    public override DepthTextureMode GetDepthTextureMode(RenderingPath renderPath)
    {
        var mode = base.GetDepthTextureMode(renderPath);

        if (renderPath == RenderingPath.Forward)
        {
            return mode | DepthTextureMode.Depth;
        }

        return mode; 
    }

    protected override RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.RFloat;
    }

    protected override string GetEffectName()
    {
        return "RaytraceVolumetricLighting";
    }

    protected override CameraEvent GetCameraEvent(RenderingPath renderPath, bool isSceneView)
    {
        if (TemporallyRenderEffect && TemporallyReproject)
        {
            return CameraEvent.AfterImageEffectsOpaque;
        }
        else if (renderPath == RenderingPath.DeferredShading)
        {
#if UNITY_EDITOR
            if (isSceneView)
            {
                return CameraEvent.AfterLighting;
            }
#endif

            return CameraEvent.AfterSkybox;
        }
        else
        {
#if UNITY_EDITOR
            if (isSceneView)
            {
                return CameraEvent.AfterForwardOpaque;
            }
#endif

            return CameraEvent.AfterSkybox;
        }
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceShadowsPass";
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "VolumetricLightingRayGen";
    }

    private void Blur(RaytraceCommandBufferData context)
    {
        var command = context.command;

        var kernelIndex = 2;

        // process the shadowmap via a compute shader 
        command.SetComputeFloatParam(BlurShader, "_ShadowStrength", ShadowStrength);
        command.SetComputeIntParam(BlurShader, "_textureWidth", _RenderTexture.width);
        command.SetComputeIntParam(BlurShader, "_textureHeight", _RenderTexture.height);

        var _VLTemp = Shader.PropertyToID("_VLTemp");
        command.GetTemporaryRT(_VLTemp, _RenderTexture.width, _RenderTexture.height, _RenderTexture.depth,
            _RenderTexture.filterMode, _RenderTexture.graphicsFormat, _RenderTexture.antiAliasing);

        command.CopyTexture(_RenderTexture, _VLTemp); 

        command.SetComputeTextureParam(BlurShader, kernelIndex, "_ShadowmapTemp", _VLTemp);

        command.SetComputeTextureParam(BlurShader, kernelIndex, "_ShadowMap", _RenderTexture);
        command.DispatchCompute(BlurShader, kernelIndex, _RenderTexture.width / 32, _RenderTexture.height / 32, 1);
    }

    protected override void ConfigureRaytraceCommands(RaytraceCommandBufferData context)
    {
        base.ConfigureRaytraceCommands(context);
        
        var command = context.command;
        command.SetRayTracingFloatParam(_RaytracingShader, "step_count", IterationSteps); 
        command.SetRayTracingFloatParam(_RaytracingShader, "step_intensity", IterationStrength * 10f); 
        command.SetRayTracingFloatParam(_RaytracingShader, "MaxShadowDistance", MaxShadowDistance); 
    }

    protected override void AppendCommandBufferAfterDispatch(RaytraceCommandBufferData context)
    {
        base.AppendCommandBufferAfterDispatch(context);

        Blur(context); 

        if (_blitMaterial == null)
        {
            _blitMaterial = new Material(BlitShader);
        }

        var camera = context.camera;
        var command = context.command;

        var renderPath = camera.actualRenderingPath;
        var isDeferred = renderPath == RenderingPath.DeferredShading;

        var projection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        var inverseProjection = projection.inverse;

        command.SetGlobalMatrix("_InverseProjection", inverseProjection);
        command.SetGlobalMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        command.SetGlobalTexture("_RaytracedVolumetricLightingTexture", _RenderTexture);

        var grabpass = Shader.PropertyToID("_RaytracedGrabpass");

        command.GetTemporaryRT(grabpass, -1, -1, 24, FilterMode.Bilinear,
            camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default,
            1, true, RenderTextureMemoryless.None);

        command.Blit(BuiltinRenderTextureType.CurrentActive, grabpass);
        command.SetGlobalTexture("_RaytracedGrabpass", grabpass);

        command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, isDeferred  ? BuiltinRenderTextureType.ResolvedDepth : BuiltinRenderTextureType.Depth);

        // if (context.renderPath == RenderingPath.DeferredShading)
        // {
        // 
        // }
        // else
        // {
        //     var reflectionsGrabpass = Shader.PropertyToID("_RaytracedGrabpass");
        // 
        //     command.GetTemporaryRT(reflectionsGrabpass, -1, -1, 24, FilterMode.Bilinear,
        //         camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1,
        //         true, RenderTextureMemoryless.None);
        // 
        //     command.Blit(BuiltinRenderTextureType.CurrentActive, reflectionsGrabpass);
        //     command.SetGlobalTexture("_RaytracedGrabpass", reflectionsGrabpass);
        // 
        //     command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        // }

        command.DrawMesh(RaytraceDataManager.fullscreenTriangle, Matrix4x4.identity, _blitMaterial, 0, 0);
    }
}
