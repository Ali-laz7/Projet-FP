using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceShadowsManager))]
public class RaytraceShadowsManagerEditor : RaytraceEffectBaseEditor
{
    // user editable properties 
    SerializedProperty ShadowQuality;
    SerializedProperty shadowBias;
    SerializedProperty shadowStrength;
    SerializedProperty smoothShadows;
    SerializedProperty shadowRaySeparation;
    SerializedProperty forwardRenderingGenerateNormals;
    SerializedProperty DeferredShadingOverride;
    SerializedProperty ForwardShadows;

    // default foldout states 
    private bool dataFoldout = false;
    private bool settingsFoldout = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        ShadowQuality = serializedObject.FindProperty("ShadowQuality");
        shadowBias = serializedObject.FindProperty("shadowBias");
        shadowStrength = serializedObject.FindProperty("shadowStrength");
        smoothShadows = serializedObject.FindProperty("smoothShadows");
        shadowRaySeparation = serializedObject.FindProperty("shadowRaySeparation");
        forwardRenderingGenerateNormals = serializedObject.FindProperty("forwardRenderingGenerateNormals");
        DeferredShadingOverride = serializedObject.FindProperty("DeferredShadingOverride");
        ForwardShadows = serializedObject.FindProperty("ForwardShadows");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (WarnUseError())
        {
            return; 
        }

        var instance = (RaytraceShadowsManager) target;
        var camera = instance.GetComponent<Camera>();
        var isDeferred = camera.actualRenderingPath == RenderingPath.DeferredShading;
        var lights = FindObjectsOfType<Light>();

        var shadowCastingDirectionalExists = false;
        for (var li = 0; li < lights.Length; ++li)
        {
            var light = lights[li];
            var directional = light.type == LightType.Directional;
            var shadowCasting = light.shadows != LightShadows.None;
                
            if(directional && shadowCasting)
            {
                shadowCastingDirectionalExists = true;
                break;
            }
        }

        if(shadowCastingDirectionalExists)
        {
            EditorGUILayout.HelpBox("When using Raytraced shadows, you need to disable shadows from the directional light.", MessageType.Warning); 
        }

        EditorGUILayout.BeginVertical("GroupBox");
            settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings", true);
        
            if(settingsFoldout)
            {
                EditorGUILayout.PropertyField(ShadowQuality);
                EditorGUILayout.PropertyField(shadowBias);
                EditorGUILayout.PropertyField(shadowStrength);
                EditorGUILayout.PropertyField(smoothShadows);
                EditorGUILayout.PropertyField(shadowRaySeparation);
                DrawSharedSettings();

                // we don't support reprojection for shadows 
                // DrawReprojectionSettings();

                if (isDeferred)
                {
                    var deferredShadingMode = GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredShading);
                    var deferredShadingShader = GraphicsSettings.GetCustomShader(BuiltinShaderType.DeferredShading);
                    if(    deferredShadingMode == BuiltinShaderMode.UseBuiltin 
                        || deferredShadingMode  == BuiltinShaderMode.Disabled 
                        || deferredShadingShader == null 
                        || deferredShadingShader.name == "Hidden/Internal-DeferredShading")
                    {
                        if(!instance.IgnoreShadingWarning)
                        {
                            EditorGUILayout.HelpBox("Due to a Unity bug, for shadows to work in Deferred, we must replace the 'Hidden/Internal-DeferredShading' shader." +
                                "If you have already done this manually, you can ignore this message. If you have not, please replace the built-in DeferredShading shader." +
                                "You can find this in the Project Settings under the Graphics section.", MessageType.Warning);

                            if (GUILayout.Button("Fix it for me!"))
                            {
                                GraphicsSettings.SetShaderMode(BuiltinShaderType.DeferredShading, BuiltinShaderMode.UseCustom);
                                GraphicsSettings.SetCustomShader(BuiltinShaderType.DeferredShading, instance.DeferredShadingOverride);
                            }

                            if(GUILayout.Button("Okay! Ignore."))
                            {
                                instance.IgnoreShadingWarning = true;
                            }
                        }
                    }

                }
                else
                {
                    EditorGUILayout.PropertyField(forwardRenderingGenerateNormals);
                }
            }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("GroupBox");

        dataFoldout = EditorGUILayout.Foldout(dataFoldout, "Data", true);
       
        if(dataFoldout)
        {
            EditorGUILayout.PropertyField(_RaytracingShader);
            EditorGUILayout.PropertyField(DeferredShadingOverride);
            EditorGUILayout.PropertyField(ForwardShadows);
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

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RaytraceShadowsManager : RaytraceEffectBase
{
    [Range(0.001f, 0.5f)] public float shadowBias = 0.001f;
    [Range(0f, 1f)] public float shadowStrength = 1f;
    [Range(0.0001f, 0.01f)] public float shadowRaySeparation = 0.001f;
    public Color shadowColor = Color.black;

    public RaytraceQuality ShadowQuality;
    public bool forwardRenderingGenerateNormals = false;
    public bool smoothShadows = false;

    public Shader DeferredShadingOverride;
    public ComputeShader ForwardShadows;

    protected override CameraEvent GetCameraEvent(RenderingPath renderPath, bool isSceneView)
    {
        if(renderPath == RenderingPath.DeferredShading)
        {
            return CameraEvent.BeforeLighting;
        }
        else
        {
            return CameraEvent.BeforeForwardOpaque;
        }
    }

    public override DepthTextureMode GetDepthTextureMode(RenderingPath renderPath)
    {
        var mode = base.GetDepthTextureMode(renderPath);
        if (renderPath == RenderingPath.Forward)
        {
            if(forwardRenderingGenerateNormals)
            {
                return mode | DepthTextureMode.DepthNormals;
            }
            else
            {
                return mode | DepthTextureMode.Depth;
            }
        }

        return mode; 
    }

    protected override RenderTextureFormat GetRenderTextureFormat()
    {
        return RenderTextureFormat.RFloat;
    }

    protected override void ConfigureRaytraceCommands(RaytraceCommandBufferData context)
    {
        base.ConfigureRaytraceCommands(context);

        var command = context.command;
        var camera = context.camera;

        command.SetGlobalFloat("_ShadowBias", shadowBias);

        switch (ShadowQuality)
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

        command.SetGlobalFloat("_RaySeparation", shadowRaySeparation);

        if(context.renderPath == RenderingPath.Forward)
        {
            if (forwardRenderingGenerateNormals)
            {
                command.EnableShaderKeyword("_HAS_DEPTH_NORMALS");
            }
            else
            {
                command.DisableShaderKeyword("_HAS_DEPTH_NORMALS");
            }
        }
    }

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        if(renderPath == RenderingPath.DeferredShading)
        {
            return "ShadowsRayGeneration_Deferred"; 
        }
        else
        {
            return "ShadowsRayGeneration_Forward";
        }
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceShadowsPass";
    }
    
    protected override void AppendCommandBufferAfterDispatch(RaytraceCommandBufferData context)
    {
        base.AppendCommandBufferAfterDispatch(context);

        var command = context.command;

        // process the shadowmap via a compute shader 
        command.SetComputeFloatParam(ForwardShadows, "_ShadowStrength", shadowStrength);
        command.SetComputeIntParam(ForwardShadows, "_textureWidth", _RenderTexture.width);
        command.SetComputeIntParam(ForwardShadows, "_textureHeight", _RenderTexture.height);

        // note: would be cool to get rid of this blit 
        var _CorgiShadowmap = Shader.PropertyToID("_CorgiShadowmap");
        command.GetTemporaryRT(_CorgiShadowmap, _RenderTexture.width, _RenderTexture.height, _RenderTexture.depth,
            _RenderTexture.filterMode, _RenderTexture.graphicsFormat, _RenderTexture.antiAliasing, true);

        command.CopyTexture(_RenderTexture, _CorgiShadowmap);

        if (smoothShadows)
        {
            command.SetComputeTextureParam(ForwardShadows, 0, "_ShadowmapTemp", _RenderTexture);
            command.SetComputeTextureParam(ForwardShadows, 0, "_ShadowMap", _CorgiShadowmap);
            command.DispatchCompute(ForwardShadows, 0, _RenderTexture.width / 32, _RenderTexture.height / 32, 1);
        }
        else
        {
            command.SetComputeTextureParam(ForwardShadows, 1, "_ShadowmapTemp", _RenderTexture);
            command.SetComputeTextureParam(ForwardShadows, 1, "_ShadowMap", _CorgiShadowmap);
            command.DispatchCompute(ForwardShadows, 1, _RenderTexture.width / 32, _RenderTexture.height / 32, 1);
        }

        // set some unity specific variables, to hack in our rtx-based shadowmap
        command.SetGlobalTexture("_ShadowMapTexture", _CorgiShadowmap);
        command.SetGlobalTexture("_RaytracingShadowMapTexture", _CorgiShadowmap);

        command.SetGlobalVector("_LightShadowData",
            new Vector4(
                shadowStrength,
                1f,
                1f / 1024f,
                0f
            )
        );

        command.EnableShaderKeyword("SHADOWS_DEPTH");
        command.EnableShaderKeyword("SHADOWS_NATIVE");
        command.EnableShaderKeyword("SHADOWS_SCREEN");
    }
}
