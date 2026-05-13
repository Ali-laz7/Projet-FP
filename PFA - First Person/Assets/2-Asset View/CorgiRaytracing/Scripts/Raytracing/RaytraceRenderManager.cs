using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceRenderManager))]
public class RaytraceRenderManagerEditor : RaytraceEffectBaseEditor
{

    // user editable properties 
    SerializedProperty BlitMaterialDeferred;
    SerializedProperty BlitMaterialForward;

    // default foldout states 
    private bool dataFoldout = false;
    private bool settingsFoldout = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        BlitMaterialDeferred = serializedObject.FindProperty("BlitMaterialDeferred");
        BlitMaterialForward = serializedObject.FindProperty("BlitMaterialForward");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (WarnUseError())
        {
            return;
        }

        var instance = (RaytraceRenderManager) target;
        var camera = instance.GetComponent<Camera>();
        var isDeferred = camera.actualRenderingPath == RenderingPath.DeferredShading;

        // settings
        EditorGUILayout.BeginVertical("GroupBox");
        settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings", true);

        if (settingsFoldout)
        {
            DrawSharedSettings();
            DrawReprojectionSettings();

        }
        EditorGUILayout.EndVertical();

        // data 

        EditorGUILayout.BeginVertical("GroupBox");

        dataFoldout = EditorGUILayout.Foldout(dataFoldout, "Data", true);

        if (dataFoldout)
        {
            if (isDeferred)
            {
                EditorGUILayout.PropertyField(BlitMaterialDeferred);
            }
            else
            {
                EditorGUILayout.PropertyField(BlitMaterialForward);
            }

            EditorGUILayout.PropertyField(_RaytracingShader);
            EditorGUILayout.PropertyField(TemporalReprojection);
        }

        EditorGUILayout.EndVertical();

        // DrawRenderTexture();



        EditorGUILayout.BeginVertical("GroupBox");
        _foundoutRenderTexture = EditorGUILayout.Foldout(_foundoutRenderTexture, "Debug", true);
        if (_foundoutRenderTexture)
        {
            if (!instance.enabled)
            {
                GUILayout.Label("Effect is currently disabled.");
            }
            else
            {
                if(isDeferred)
                {
                    for(var i = 0; i < instance._GBuffers.Length; ++i)
                    {
                        DrawSingleRT(instance._GBuffers[i]);
                    }
                }
                else
                {
                    DrawSingleRT(instance._RenderTexture);
                }
            }
        }
        EditorGUILayout.EndVertical();

        // save 
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawSingleRT(RenderTexture tex)
    {
        if (tex == null)
        {
            GUILayout.Label("Note: GameView must be visible.");
        }
        else
        {
            var labelStyle = GUI.skin.GetStyle("Label");
            var style = new GUIStyle(labelStyle);

            style.alignment = TextAnchor.MiddleCenter;
            style.fixedWidth = tex.width * 0.25f;
            style.fixedHeight = tex.height * 0.25f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(tex, style);
            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif

public class RaytraceRenderManager : RaytraceEffectBase
{
    public Material BlitMaterialDeferred;
    public Material BlitMaterialForward;

    // pretty wasteful on memory.. 
    [System.NonSerialized] public RenderTexture[] _GBuffers = new RenderTexture[4]; // GBuffer0 is colors 
                                                                                    // GBuffer1 is specular
                                                                                    // GBuffer2 is normals 
                                                                                    // GBuffer3 is emission 

    protected override void OnDisable()
    {
        base.OnDisable();

        for (var i = 0; i < _GBuffers.Length; ++i)
        {
            if (_GBuffers[0] != null)
            {
                _GBuffers[0].Release();
                _GBuffers[0] = null;
            }
        }
    }

    protected override void ConfigureRaytraceCommands(RaytraceCommandBufferData context)
    {
        base.ConfigureRaytraceCommands(context);

        if(context.renderPath == RenderingPath.DeferredShading)
        {
            var command = context.command; 
            command.SetRayTracingTextureParam(_RaytracingShader, "RenderTarget0", _GBuffers[0]);
            command.SetRayTracingTextureParam(_RaytracingShader, "RenderTarget1", _GBuffers[1]);
            command.SetRayTracingTextureParam(_RaytracingShader, "RenderTarget2", _GBuffers[2]);
            command.SetRayTracingTextureParam(_RaytracingShader, "RenderTarget3", _GBuffers[3]);
        }
    }

    protected override void EnsureRT(RaytraceCommandBufferData context)
    {
        base.EnsureRT(context);

        if(context.renderPath == RenderingPath.DeferredShading)
        {
            var camera = context.camera;

            var resolution = Mathf.Max(camera.pixelWidth / TextureScaleReciprocal, camera.pixelHeight / TextureScaleReciprocal);
            resolution = Mathf.ClosestPowerOfTwo(resolution);

            var mipCount = GetRenderTextureMipMapCount();

            for (var i = 0; i < _GBuffers.Length; ++i)
            {
                var renderTexture = _GBuffers[i];

                if (renderTexture == null || renderTexture.width != resolution || renderTexture.mipmapCount != mipCount)
                {
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                    }

                    renderTexture = new RenderTexture(resolution, resolution, 24, GetRenderTextureFormat(), mipCount);
                    renderTexture.useMipMap = mipCount > 1;
                    renderTexture.autoGenerateMips = false;
                    renderTexture.filterMode = GetRenderTextureFilterMode();
                    renderTexture.enableRandomWrite = true;
                    renderTexture.anisoLevel = AnisoLevelSetting;
                    renderTexture.antiAliasing = AntiAliasSetting;

                    renderTexture.Create();
                }

                _GBuffers[i] = renderTexture;
            }
        }
    }

    protected override string GetEffectName()
    {
        return "RaytraceRender";
    }

    protected override RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.ARGBFloat;
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceRenderPass";
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        if(renderPath == RenderingPath.DeferredShading)
        {
            return "RaytraceRenderRayGeneration_Deferred";
        }
        else
        {
            return "RaytraceRenderRayGeneration_Forward";
        }
    }

    protected override CameraEvent GetCameraEvent(RenderingPath renderPath, bool isSceneView)
    {
        if(renderPath == RenderingPath.DeferredShading)
        {
            return CameraEvent.AfterGBuffer;
        }
        else
        {
            return CameraEvent.AfterForwardAlpha;
        }
    }

    protected override void AppendCommandBufferAfterDispatch(RaytraceCommandBufferData context)
    {
        base.AppendCommandBufferAfterDispatch(context);

        var command = context.command;
        var camera = context.camera;
        var renderPath = context.renderPath;

        if(renderPath == RenderingPath.DeferredShading)
        {

            command.SetGlobalTexture("_CopyBlitTex0", _GBuffers[0]);
            command.SetGlobalTexture("_CopyBlitTex1", _GBuffers[1]);
            command.SetGlobalTexture("_CopyBlitTex2", _GBuffers[2]);
            command.SetGlobalTexture("_CopyBlitTex3", _GBuffers[3]);

            command.SetRenderTarget(new RenderTargetIdentifier[]
                {
                 BuiltinRenderTextureType.GBuffer0,
                 BuiltinRenderTextureType.GBuffer1,
                 BuiltinRenderTextureType.GBuffer2,

                 BuiltinRenderTextureType.CurrentActive
                }
                , BuiltinRenderTextureType.CameraTarget);

            command.DrawMesh(RaytraceDataManager.fullscreenTriangle, Matrix4x4.identity, BlitMaterialDeferred, 0, 0);
        }
        else
        {
            command.SetGlobalTexture("_CopyBlitTex", _RenderTexture);
            command.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);

            command.DrawMesh(RaytraceDataManager.fullscreenTriangle, Matrix4x4.identity, BlitMaterialForward, 0, 0);
        }
    }
}
