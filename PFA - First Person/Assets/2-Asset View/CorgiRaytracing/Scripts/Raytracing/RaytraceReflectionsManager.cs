using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceReflectionsManager))]
public class RaytraceReflectionsManagerEditor : RaytraceEffectBaseEditor
{

    // user editable properties 
    SerializedProperty skybox;
    SerializedProperty useBlurredMips;
    SerializedProperty Bounces;
    SerializedProperty Roughness;
    SerializedProperty ShadowsInReflections;
    SerializedProperty DeferredReflections;
    SerializedProperty ForwardReflections;
    SerializedProperty FallbackSkybox;
    SerializedProperty gaussianDownsample;
    SerializedProperty ReflectionsHaveFogInDeferred;

    // default foldout states 
    private bool dataFoldout = false;
    private bool settingsFoldout = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        skybox = serializedObject.FindProperty("skybox");
        useBlurredMips = serializedObject.FindProperty("useBlurredMips");
        Bounces = serializedObject.FindProperty("Bounces");
        Roughness = serializedObject.FindProperty("Roughness");
        ShadowsInReflections = serializedObject.FindProperty("ShadowsInReflections");
        DeferredReflections = serializedObject.FindProperty("DeferredReflections");
        ForwardReflections = serializedObject.FindProperty("ForwardReflections");
        FallbackSkybox = serializedObject.FindProperty("FallbackSkybox");
        gaussianDownsample = serializedObject.FindProperty("gaussianDownsample");
        ReflectionsHaveFogInDeferred = serializedObject.FindProperty("ReflectionsHaveFogInDeferred");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); 
        
        if(WarnUseError())
        {
            return; 
        }

        var instance = (RaytraceReflectionsManager) target;
        var camera = instance.GetComponent<Camera>();
        var isDeferred = camera.actualRenderingPath == RenderingPath.DeferredShading;

        // settings
        EditorGUILayout.BeginVertical("GroupBox");
        settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings", true);

        if (settingsFoldout)
        {
            EditorGUILayout.PropertyField(skybox);
            EditorGUILayout.PropertyField(Bounces);
            EditorGUILayout.PropertyField(Roughness);
            EditorGUILayout.PropertyField(ShadowsInReflections);
            DrawSharedSettings();
            DrawReprojectionSettings();

            if (isDeferred)
            {
                EditorGUILayout.PropertyField(ReflectionsHaveFogInDeferred);

                if(instance.gaussianDownsample != null)
                {
                    EditorGUILayout.PropertyField(useBlurredMips);
                }
                else
                {

                }
            }
        }
        EditorGUILayout.EndVertical();

        // data 

        EditorGUILayout.BeginVertical("GroupBox");

        dataFoldout = EditorGUILayout.Foldout(dataFoldout, "Data", true);

        if (dataFoldout)
        {
            EditorGUILayout.PropertyField(_RaytracingShader);
            EditorGUILayout.PropertyField(DeferredReflections);
            EditorGUILayout.PropertyField(ForwardReflections);
            EditorGUILayout.PropertyField(FallbackSkybox);
            EditorGUILayout.PropertyField(gaussianDownsample);
            EditorGUILayout.PropertyField(TemporalReprojection);
        }

        EditorGUILayout.EndVertical();

        DrawRenderTexture();

        // save 
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif


[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RaytraceReflectionsManager : RaytraceEffectBase
{
    public Texture skybox;
    public bool useBlurredMips;
    public bool ReflectionsHaveFogInDeferred;

    public RaytraceQuality Bounces;
    public RaytraceQuality Roughness;
    public RaytraceQuality ShadowsInReflections;

    public Shader DeferredReflections;
    public Shader ForwardReflections;
    public Texture FallbackSkybox;

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

    protected override CameraEvent GetCameraEvent(RenderingPath renderPath, bool isSceneView)
    {
        if(TemporallyRenderEffect && TemporallyReproject)
        {
            return CameraEvent.AfterImageEffectsOpaque;
        }
        else if(renderPath == RenderingPath.DeferredShading)
        {
            return CameraEvent.AfterReflections;
        }
        else
        {
            return CameraEvent.AfterForwardAlpha;
        }
    }
    
    protected override string GetEffectName()
    {
        return "RaytraceReflections";
    }

    protected override RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.ARGBFloat;
    }

    protected override int GetRenderTextureMipMapCount()
    {
        return useBlurredMips ? 6 : 1;
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceReflectionPass";
    }

    protected override void ConfigureRaytraceCommands(RaytraceCommandBufferData context)
    {
        base.ConfigureRaytraceCommands(context);

        var command = context.command;
        var camera = context.camera;

        var bounce_count_offset = context.renderPath == RenderingPath.DeferredShading ? 0f : -1f;

        switch (Bounces)
        {
            default:
            case RaytraceQuality.Low:
                float low_count = 3f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", low_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (low_count + 1));
                break;
            case RaytraceQuality.Med:
                float med_count = 5f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", med_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (med_count + 1));
                break;
            case RaytraceQuality.High:
                float high_count = 8f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", high_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (high_count + 1));
                break;
            case RaytraceQuality.Overkill:
                float overkill_count = 16f + bounce_count_offset;
                command.SetGlobalFloat("MAX_BOUNCES", overkill_count);
                command.SetGlobalFloat("MAX_BOUNCES_RIC", 1f / (overkill_count + 1));
                break;
        }

        switch (Roughness)
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

        switch (ShadowsInReflections)
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
        
        if (skybox == null)
        {
            skybox = FallbackSkybox;
        }

        command.SetRayTracingTextureParam(_RaytracingShader, "SkyboxTex", skybox);

        if(ReflectionsHaveFogInDeferred || camera.renderingPath == RenderingPath.Forward)
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
        if(renderPath == RenderingPath.DeferredShading)
        {
            return "ReflectionRayGeneration_Deferred";
        }
        else
        {
            return "ReflectionRayGeneration_Forward";
        }
    }

    protected override void AppendCommandBufferAfterDispatch(RaytraceCommandBufferData context)
    {
        base.AppendCommandBufferAfterDispatch(context);

        var command = context.command;
        var camera = context.camera;

        var projection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        var inverseProjection = projection.inverse;

        if (useBlurredMips && context.renderPath == RenderingPath.DeferredShading)
        {
            GenerateLODs(command);
        }
        
        if(_blitMaterial == null)
        {
            _blitMaterial = new Material(context.renderPath == RenderingPath.DeferredShading
                ? DeferredReflections : ForwardReflections);
        }

        // process 
        _blitMaterial.shader = camera.actualRenderingPath == RenderingPath.DeferredShading
            ? DeferredReflections
            : ForwardReflections;


        command.SetGlobalMatrix("corgi_InverseProjection", inverseProjection);
        command.SetGlobalMatrix("corgi_CameraToWorld", camera.cameraToWorldMatrix);
        command.SetGlobalTexture("_RTXReflectionsTex", _RenderTexture);

        if(TemporallyRenderEffect && TemporalReprojection)
        {

            var reflectionsGrabpass = Shader.PropertyToID("_ReflectionsGrabpass");

            command.GetTemporaryRT(reflectionsGrabpass, -1, -1, 24, FilterMode.Bilinear,
                camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1,
                true, RenderTextureMemoryless.None);

            command.Blit(BuiltinRenderTextureType.CurrentActive, reflectionsGrabpass);
            command.SetGlobalTexture("_ReflectionGrabpass", reflectionsGrabpass);

            command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        }
        else if (context.renderPath == RenderingPath.DeferredShading)
        {

        }
        else
        {
            var reflectionsGrabpass = Shader.PropertyToID("_ReflectionsGrabpass");

            command.GetTemporaryRT(reflectionsGrabpass, -1, -1, 24, FilterMode.Bilinear,
                camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1,
                true, RenderTextureMemoryless.None);

            command.Blit(BuiltinRenderTextureType.CurrentActive, reflectionsGrabpass);
            command.SetGlobalTexture("_ReflectionGrabpass", reflectionsGrabpass);

            command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        }

        var existingVL = GetComponent<RaytraceEffectVolumetricLighting>();
        if(TemporallyRenderEffect && TemporallyReproject && existingVL != null && existingVL.enabled)
        {
            command.EnableShaderKeyword("_ReflectionsNeedVL");
        }
        else
        {
            command.DisableShaderKeyword("_ReflectionsNeedVL");
        }

        command.DrawMesh(RaytraceDataManager.fullscreenTriangle, Matrix4x4.identity, _blitMaterial, 0, 0);
    }

    // todo, our own gaussian solution 
    private int[] m_MipIDs;
    public ComputeShader gaussianDownsample;

    public void GenerateLODs(CommandBuffer command)
    {
        if(gaussianDownsample == null)
        {
            return;
        }

        int kMaxLods = 6;
        if (m_MipIDs == null || m_MipIDs.Length != _RenderTexture.mipmapCount)
        {
            m_MipIDs = new int[kMaxLods];

            for (int i = 0; i < kMaxLods; i++)
                m_MipIDs[i] = Shader.PropertyToID("_GaussianMip" + i);
        }

        var compute = gaussianDownsample;
        int kernel = compute.FindKernel("KMain");
        var mipFormat = _RenderTexture.format;

        var last = new RenderTargetIdentifier(_RenderTexture, 0);

        int lodCount = kMaxLods - 1;

        int width = _RenderTexture.width;
        int height = _RenderTexture.height;

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
