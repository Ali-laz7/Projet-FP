using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceEffectBase))]
public class RaytraceEffectBaseEditor : Editor
{
    protected SerializedProperty _RaytracingShader;
    protected SerializedProperty TemporallyRenderEffect;
    protected SerializedProperty TemporalEffectFrameDuration;
    protected SerializedProperty TemporallyReproject;
    protected SerializedProperty TemporalReprojection;
    protected SerializedProperty AntiAliasSetting;
    protected SerializedProperty AnisoLevelSetting;
    protected SerializedProperty TextureScaleReciprocal;
    protected SerializedProperty MaxRayDistance;
    protected SerializedProperty _RayFlags;

    protected virtual void OnEnable()
    {
        _RaytracingShader = serializedObject.FindProperty("_RaytracingShader");
        TemporallyRenderEffect = serializedObject.FindProperty("TemporallyRenderEffect");
        TemporalEffectFrameDuration = serializedObject.FindProperty("TemporalEffectFrameDuration");
        TemporallyReproject = serializedObject.FindProperty("TemporallyReproject");
        TemporalReprojection = serializedObject.FindProperty("TemporalReprojection");
        AntiAliasSetting = serializedObject.FindProperty("AntiAliasSetting");
        AnisoLevelSetting = serializedObject.FindProperty("AnisoLevelSetting");
        TextureScaleReciprocal = serializedObject.FindProperty("TextureScaleReciprocal");
        MaxRayDistance = serializedObject.FindProperty("MaxRayDistance");
        _RayFlags = serializedObject.FindProperty("_RayFlags");
    }

    protected bool WarnUseError()
    {
        var camera = (target as MonoBehaviour).GetComponent<Camera>();

        if (camera == null)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.HelpBox("A camera is required.", MessageType.Error);
            EditorGUILayout.EndVertical();
            return true;
        }

        if (camera.actualRenderingPath != RenderingPath.DeferredShading && camera.actualRenderingPath != RenderingPath.Forward)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.HelpBox("The camera must use either Deferred or Forward rendering (not legacy!).",
                MessageType.Error);
            EditorGUILayout.EndVertical();
            return true;
        }

        if (RaytraceDataManager.Instance == null)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.HelpBox("RaytraceDataManager has not yet been placed in the scene.",
                MessageType.Error);
            EditorGUILayout.EndVertical();
            return true;
        }

        return false;
    }

    protected bool _foundoutRenderTexture;

    protected virtual void DrawRenderTexture()
    {

        EditorGUILayout.BeginVertical("GroupBox");
            _foundoutRenderTexture = EditorGUILayout.Foldout(_foundoutRenderTexture, "Debug", true);
            if(_foundoutRenderTexture)
            {
                var instance = (RaytraceEffectBase)target;
                var tex = instance._RenderTexture;

                if(!instance.enabled)
                {
                    GUILayout.Label("Effect is currently disabled.");
                }

                else if(tex == null)
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
        EditorGUILayout.EndVertical();
    }

    protected void DrawReprojectionSettings()
    {
        EditorGUILayout.PropertyField(TemporallyRenderEffect);

        if (TemporallyRenderEffect.boolValue)
        {
            EditorGUILayout.PropertyField(TemporallyReproject);
            EditorGUILayout.PropertyField(TemporalEffectFrameDuration);

            if(TemporallyReproject.boolValue && TemporalEffectFrameDuration.intValue > 1)
            {
                EditorGUILayout.HelpBox("Note: Reprojection does not work correctly when TemporalEffectFrameDuration is over 1.", MessageType.Warning);
            }

            if(TemporallyReproject.boolValue)
            {
                EditorGUILayout.HelpBox("Note: Temporal reprojection is disabled on the scene camera (use the Game view).", MessageType.Info);
            }
        }
    }

    protected void DrawSharedSettings()
    {
        EditorGUILayout.PropertyField(MaxRayDistance);
        EditorGUILayout.PropertyField(_RayFlags);

        EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("Render Texture Settings");
            EditorGUILayout.PropertyField(TextureScaleReciprocal);
            // EditorGUILayout.PropertyField(AntiAliasSetting);
            EditorGUILayout.PropertyField(AnisoLevelSetting);
        EditorGUILayout.EndVertical(); 
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(WarnUseError())
        {
            return;
        }

        DrawRenderTexture(); 
    }
}

#endif

[ExecuteAlways, ImageEffectAllowedInSceneView, RequireComponent(typeof(RaytraceEffectRenderer)), RequireComponent(typeof(Camera))]
public class RaytraceEffectBase : MonoBehaviour
{
    public RayTracingShader _RaytracingShader;

    [HideInInspector] public bool IgnoreShadingWarning = false;

    [Range(1, 5)] public int TextureScaleReciprocal = 1;
    [System.NonSerialized] public RaytraceEffectRenderer RaytraceRenderer;
    [System.NonSerialized] public RenderTexture _RenderTexture;
    [System.NonSerialized] public RaytraceCommandBufferData Data = null;
    [System.NonSerialized] public Camera _camera;
    [System.NonSerialized] private bool _supportsRayTracing;
    public ComputeShader TemporalReprojection;

    public bool TemporallyRenderEffect;
    public bool TemporallyReproject;
    [Range(1, 8)] public int TemporalEffectFrameDuration = 4;
    [Range(1, 8)] public int AntiAliasSetting = 1;
    [Range(0, 8)] public int AnisoLevelSetting = 1;
    [Range(1f, 1000f)] public float MaxRayDistance = 100f;

    // https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html#ray-flags
    [System.Flags]
    public enum RayFlags
    {
        RAY_FLAG_NONE = 0x00,
        RAY_FLAG_FORCE_OPAQUE = 0x01,
        RAY_FLAG_FORCE_NON_OPAQUE = 0x02,
        RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH = 0x04,
        RAY_FLAG_SKIP_CLOSEST_HIT_SHADER = 0x08,
        RAY_FLAG_CULL_BACK_FACING_TRIANGLES = 0x10,
        RAY_FLAG_CULL_FRONT_FACING_TRIANGLES = 0x20,
        RAY_FLAG_CULL_OPAQUE = 0x40,
        RAY_FLAG_CULL_NON_OPAQUE = 0x80,
        RAY_FLAG_SKIP_TRIANGLES = 0x100,
        RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES = 0x200,
    }

    public RayFlags _RayFlags = RayFlags.RAY_FLAG_CULL_BACK_FACING_TRIANGLES;

    protected virtual void OnEnable()
    {

        previous_SetOnce = false;

        _camera = GetComponent<Camera>();

        RaytraceRenderer = GetComponent<RaytraceEffectRenderer>();
        if(RaytraceRenderer != null)
        {
            RaytraceRenderer.RegisterEffect(this);
        }

        // just in case.. 
        _supportsRayTracing = SystemInfo.supportsRayTracing;

        if (!_supportsRayTracing)
        {
            Debug.LogError("This platform does not support Raytracing!");

#if UNITY_EDITOR
            Debug.LogError("Make sure you're in DirectX12 mode, with a GPU that supports RTX.");
#endif

            return;
        }


#if URP_INSTALLED
        var _isURP = GraphicsSettings.renderPipelineAsset is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (_isURP)
        {
            Debug.LogError("You are using URP. Instead of using the Corgi Raytracing scripts, please use the Corgi Raytracing RenderFeatures instead!");
            _supportsRayTracing = false; 
            return; 
        }
#endif
    }

    protected virtual void OnDisable()
    {
        if (Data != null)
        {
            if (Data.camera != null)
            {
                Data.camera.RemoveCommandBuffer(Data.cameraEvent, Data.command);
            }


            Data.command.Release();
            Data = null; 
        }

        if(command_reprojection != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, command_reprojection);
            command_reprojection.Release();
            command_reprojection = null;
        }

        if (_RenderTexture != null)
        {
            _RenderTexture.Release();
            _RenderTexture = null;
        }

        if(RaytraceRenderer != null)
        {
            RaytraceRenderer.UnregisterEffect(this); 
        }
    }

    private bool _previouslyTemporallyRendering;
    private bool _previouslyTemporallyReprojecting;

    private void OnPreCull()
    {
        if (!_supportsRayTracing)
        {
            return;
        }

        if (RaytraceDataManager.Instance == null || !RaytraceDataManager.Instance._accelerationStructureReady)
        {
            return;
        }

        if(_RaytracingShader == null)
        {
            return; 
        }


#if UNITY_EDITOR
        var isSceneViewCamera = false;
        var sceneViewCameras = SceneView.GetAllSceneCameras();
        var sceneViewCameraCount = sceneViewCameras.Length;
        for(var i = 0; i < sceneViewCameraCount; ++i)
        {
            var sceneViewCamera = sceneViewCameras[i];
            if(sceneViewCamera == _camera)
            {
                isSceneViewCamera = true;
                break;
            }
        }

        if(isSceneViewCamera)
        {
            TemporallyReproject = false; 
        }
#endif

        var cameraRenderPath = _camera.actualRenderingPath;

        var exists = Data != null;

        if (exists)
        {
            var reset = Data.renderPath != cameraRenderPath
                || _previouslyTemporallyRendering != TemporallyRenderEffect
                || _previouslyTemporallyReprojecting != TemporallyReproject;

            _previouslyTemporallyRendering = TemporallyRenderEffect;
            _previouslyTemporallyReprojecting = TemporallyReproject;

            // render path changed between frames? 
            if (reset)
            {
                // reset 
                OnDisable();
                OnEnable();

                exists = false;
            }
        }

        if (!exists)
        {
            _previouslyTemporallyRendering = TemporallyRenderEffect;
            _previouslyTemporallyReprojecting = TemporallyReproject;

            Data = new RaytraceCommandBufferData();
            Data.camera = _camera;
            Data.renderPath = cameraRenderPath;

            var isSceneCamera = false;

#if UNITY_EDITOR
            var sceneCameras = SceneView.GetAllSceneCameras();
            foreach(var sceneCamera in sceneCameras)
            {
                if(sceneCamera == _camera)
                {
                    isSceneCamera = true;
                    break;
                }
            }
#endif

            Data.command = new CommandBuffer();
            Data.command.name = GetEffectName();
            Data.cameraEvent = GetCameraEvent(cameraRenderPath, isSceneCamera);

            _camera.AddCommandBuffer(Data.cameraEvent, Data.command);

            if(TemporallyRenderEffect && TemporallyReproject)
            {
                command_reprojection = new CommandBuffer();
                command_reprojection.name = GetEffectName() + "_Reprojection";

                EnsureRT(Data);
                BuildTemporalReprojection(command_reprojection, _camera);


                

                _camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, command_reprojection);
            }
        }

        EnsureRT(Data);
        BuildCommandBuffer(Data);

    }

    private CommandBuffer command_reprojection;

    protected virtual void EnsureRT(RaytraceCommandBufferData context)
    {
        var camera = context.camera;

        var resolution = Mathf.Max(camera.pixelWidth / TextureScaleReciprocal, camera.pixelHeight / TextureScaleReciprocal);
            resolution = Mathf.ClosestPowerOfTwo(resolution);

        var mipCount = GetRenderTextureMipMapCount();

        if (_RenderTexture == null || _RenderTexture.width != resolution || _RenderTexture.mipmapCount != mipCount)
        {
            if (_RenderTexture != null)
            {
                _RenderTexture.Release();
            }

            _RenderTexture = new RenderTexture(resolution, resolution, 24, GetRenderTextureFormat(), mipCount);
            _RenderTexture.useMipMap = mipCount > 1;
            _RenderTexture.autoGenerateMips = false;
            _RenderTexture.filterMode = GetRenderTextureFilterMode();
            _RenderTexture.enableRandomWrite = true;
            _RenderTexture.anisoLevel = AnisoLevelSetting;
            _RenderTexture.antiAliasing = AntiAliasSetting;

            _RenderTexture.Create();
        }
    }

    protected virtual CameraEvent GetCameraEvent(RenderingPath renderPath, bool isSceneView)
    {
        return CameraEvent.AfterEverything;
    }

    protected virtual string GetEffectName()
    {
        return "RaytraceEffectBase";
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

    public virtual DepthTextureMode GetDepthTextureMode(RenderingPath renderPath)
    {
        if (TemporallyRenderEffect && TemporalReprojection)
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }
        else
        {
            return DepthTextureMode.None;
        }
    }

    protected virtual void ConfigureRaytraceCommands(RaytraceCommandBufferData context)
    {
        var command = context.command;
        var camera = context.camera;

        GetRaytracingAccelerationStructureData(out RayTracingAccelerationStructure structure, out string structureName); 

        command.SetRayTracingAccelerationStructure(_RaytracingShader, structureName, structure);
        command.SetGlobalInt("_RaytraceAgainstLayers", RaytraceDataManager.Instance.UpdateLayers.value); 
        command.SetGlobalInt("_RayFlags", (int) _RayFlags); 
        command.SetGlobalFloat("_MaxRayDistance", MaxRayDistance); 
        command.SetRayTracingShaderPass(_RaytracingShader, GetRaytracingShaderPassName());

        var projection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        var inverseProjection = projection.inverse;

        command.SetRayTracingMatrixParam(_RaytracingShader, "_InverseProjection", inverseProjection);
        command.SetRayTracingMatrixParam(_RaytracingShader, "UNITY_MATRIX_VP", camera.previousViewProjectionMatrix);
        command.SetRayTracingMatrixParam(_RaytracingShader, "_CameraToWorld", camera.cameraToWorldMatrix);
        command.SetRayTracingVectorParam(_RaytracingShader, "_WorldSpaceCameraPos", camera.transform.position);
        command.SetRayTracingTextureParam(_RaytracingShader, "RenderTarget", _RenderTexture);

        // temporal stuff 
        command.SetRayTracingIntParam(_RaytracingShader, "TemporallyRendered", TemporallyRenderEffect ? 1 : 0);

        if(TemporallyRenderEffect)
        {
            _temporal_pass_index++;
            if (_temporal_pass_index > TemporalEffectFrameDuration)
            {
                _temporal_pass_index = 0;
            }

            command.SetRayTracingIntParam(_RaytracingShader, "TemporalPassIndex", _temporal_pass_index);
            command.SetRayTracingIntParam(_RaytracingShader, "TemporalPassCount", TemporalEffectFrameDuration);
        }
    }

    protected virtual void DispatchRays(RaytraceCommandBufferData context)
    {
        var command = context.command;
        var camera = context.camera;
        var renderPath = context.renderPath;

        command.DispatchRays(_RaytracingShader, GetRaygenShaderName(renderPath), (uint) _RenderTexture.width, (uint) _RenderTexture.height, 1u, camera);
    }

    protected virtual void BuildCommandBuffer(RaytraceCommandBufferData context)
    {
        context.command.Clear();

        ConfigureRaytraceCommands(context);
        DispatchRays(context);
        AppendCommandBufferAfterDispatch(context); 
    }

    protected int _temporal_pass_index = 0;
    protected bool previous_SetOnce;
    protected Matrix4x4 previous_CameraToWorld;
    protected Matrix4x4 previous_InverseProjection;

    protected void BuildTemporalReprojection(CommandBuffer command, Camera camera)
    {
        // temporal reprojection
        if (TemporallyRenderEffect && TemporallyReproject && TemporalReprojection != null)
        {
            command.Clear();

            // note: would be cool to get rid of this blit 
            var _Temporal = Shader.PropertyToID("_Temporal" + GetEffectName());
            command.GetTemporaryRT(_Temporal, _RenderTexture.width, _RenderTexture.height, _RenderTexture.depth,
                _RenderTexture.filterMode, _RenderTexture.graphicsFormat, _RenderTexture.antiAliasing, true);

            command.CopyTexture(_RenderTexture, 0, 0, _Temporal, 0, 0);

            var kernal_reprojection = 0;
            command.SetComputeTextureParam(TemporalReprojection, kernal_reprojection, "Input", _Temporal);
            command.SetComputeTextureParam(TemporalReprojection, kernal_reprojection, "Output", _RenderTexture);

            command.SetComputeIntParam(TemporalReprojection, "texture_width", _RenderTexture.width);
            command.SetComputeIntParam(TemporalReprojection, "texture_height", _RenderTexture.height);

            command.SetComputeIntParam(TemporalReprojection, "TemporalPassIndex", _temporal_pass_index);
            command.SetComputeIntParam(TemporalReprojection, "TemporalPassCount", TemporalEffectFrameDuration);

            command.SetComputeTextureParam(TemporalReprojection, kernal_reprojection, "_CameraMotionVectorsTexture", 
                BuiltinRenderTextureType.MotionVectors);

            command.SetComputeVectorParam(TemporalReprojection, "_CameraMotionVectorsTexture_Resolution", new Vector4(camera.scaledPixelWidth, camera.scaledPixelHeight));

            command.DispatchCompute(TemporalReprojection, kernal_reprojection, _RenderTexture.width / 32, _RenderTexture.height / 32, 1);

        }
    }

    protected virtual void AppendCommandBufferAfterDispatch(RaytraceCommandBufferData context)
    {

    }
}
