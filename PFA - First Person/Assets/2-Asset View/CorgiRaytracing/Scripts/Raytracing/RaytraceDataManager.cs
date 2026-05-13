using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[Serializable]
public enum RaytraceQuality
{
    Low,
    Med,
    High,
    Overkill
}

[ExecuteAlways]
public class RaytraceDataManager : MonoBehaviour
{
    // singleton handler 
    public static RaytraceDataManager Instance;
    public static bool _warnedMultiple;

    public LayerMask UpdateLayers = 0xFFFFFFF;

    // user settings 
#if UNITY_2019_4 || UNITY_2020_1
    public bool UpdateAccelerationStructureEveryFrame = true;
#endif

    public bool BuildAccelerationStructureEveryFrame = true;
    public bool DoNotDestroyOnSceneChange = true;

    // generated 
    [NonSerialized] private bool _supportsRayTracing;
    [NonSerialized] public RayTracingAccelerationStructure _AccelerationStructure;
    [NonSerialized] public bool _accelerationStructureReady;

    private void OnEnable()
    {
        // Application.targetFrameRate = 30;  // debug 

        _accelerationStructureReady = false;

        if (Instance != null && Instance != this)
        {
            if (!_warnedMultiple)
            {
                Debug.LogWarning("Destroyed a secondary RaytraceDataManager! Try not to have multiple loaded at once.");
                _warnedMultiple = true;
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;


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


        if (Application.isPlaying)
        {
            if (DoNotDestroyOnSceneChange)
            {
                transform.SetParent(null); // dont capture parent
                DontDestroyOnLoad(gameObject);
            }
        }

        // safe to make stuff 🎉
        if (_AccelerationStructure == null)
        {
            var settings = new RayTracingAccelerationStructure.RASSettings();
            settings.layerMask = UpdateLayers;
            settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
            settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

            _AccelerationStructure = new RayTracingAccelerationStructure(settings);
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (!_supportsRayTracing)
        {
            return;
        }

        // toggle disable/enable to force free any buffers
        var reflectionManagers = FindObjectsOfType<RaytraceReflectionsManager>();
        foreach (var manager in reflectionManagers)
        {
            manager.enabled = false;
            manager.enabled = true;
        }

        var shadowsManagers = FindObjectsOfType<RaytraceShadowsManager>();
        foreach (var manager in shadowsManagers)
        {
            manager.enabled = false;
            manager.enabled = true;
        }

        // the above misses the scene camera 🙄
#if UNITY_EDITOR
        var sceneCameras = UnityEditor.SceneView.GetAllSceneCameras();
        foreach (var sceneCamera in sceneCameras)
        {
            var rrm = sceneCamera.transform.GetComponent<RaytraceReflectionsManager>();
            if (rrm != null)
            {
                rrm.enabled = false;
                rrm.enabled = true;
            }

            var rsm = sceneCamera.transform.GetComponent<RaytraceShadowsManager>();
            if (rsm != null)
            {
                rsm.enabled = false;
                rsm.enabled = true;
            }
        }
#endif

        // get rid of it 
        Release();
    }

    public void Release()
    {
        if (_accelerationStructureReady)
        {
            _AccelerationStructure.Release();
            _AccelerationStructure = null;

            _accelerationStructureReady = false;
        }
    }

    private void LateUpdate()
    {
        if (!_supportsRayTracing)
        {
            return;
        }

#if UNITY_2019_4 || UNITY_2020_1
        if (UpdateAccelerationStructureEveryFrame)
        {
            _AccelerationStructure.Update();
        }
#endif

        if (BuildAccelerationStructureEveryFrame)
        {
            _AccelerationStructure.Build();
        }

        _accelerationStructureReady = true;
    }

    // helper stuff
    static Mesh s_FullscreenTriangle;

    public static Mesh fullscreenTriangle
    {
        get
        {
            if (s_FullscreenTriangle != null)
                return s_FullscreenTriangle;

            s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

            // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
            // this directly in the vertex shader using vertex ids :(
            s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
            s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
            s_FullscreenTriangle.UploadMeshData(false);

            return s_FullscreenTriangle;
        }
    }

    public void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material blitMat)
    {
        cmd.SetGlobalTexture("_MainTex", source);
        cmd.SetRenderTarget(destination);
        cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, blitMat, 0, 0);
    }

    public static bool IsReady()
    {
        if(Instance == null)
        {
            return false;
        }

        return Instance._accelerationStructureReady;
    }
}
