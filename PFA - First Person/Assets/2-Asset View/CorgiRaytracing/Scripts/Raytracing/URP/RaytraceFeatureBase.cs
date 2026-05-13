#if URP_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RaytraceFeatureBase : ScriptableRendererFeature
{
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

    [System.Serializable]
    public class RaytraceFeatureSettings
    {
        [Header("Data"), Tooltip("defaults located in CorgiRaytracing/Data")]
        public URPRaytraceFeatureBaseData Data;

        [Header("RTX Settings"), Tooltip("these flags are used in the raytracing shader directly to affect how all intersections are handled")]
        public RayFlags _RayFlags = RayFlags.RAY_FLAG_CULL_BACK_FACING_TRIANGLES;

        [Header("RTX Quality Settings")]
        [Range(1, 5), Tooltip("A higher value will have better performance but worse visuals.")] public int TextureScaleReciprocal = 1;
        // [Range(1, 8), Tooltip("A higher value will result in worse performance but better visuals.")] public int AntiAliasSetting = 1;
        // [Range(0, 8), Tooltip("A higher value will result in worse performance but better visuals")] public int AnisoLevelSetting = 1;
        [Range(1f, 1000f), Tooltip("This is the 'view distance' of rays. Higher values will have worse performance.")] public float MaxRayDistance = 100f;
    }

    protected virtual RaytraceFeatureBase.RaytraceFeatureSettings GetSettings()
    {
        return null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // note: override in child classes 
    }

    public override void Create()
    {
        // note: override in child classes 
    }

    public virtual RenderPassEvent WhenToInsert()
    {
        return RenderPassEvent.AfterRenderingSkybox;
    }

    private bool _disabledFromMSAAMessage;

    protected bool CheckDisableIfMSAA(ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1)
        {
            if(!_disabledFromMSAAMessage)
            {
                Debug.LogWarning($"Disabled {name} due to MSAA being enabled in the URP settings. Currently, MSAA is not supported with Corgi Raytracing. Sorry!");
                _disabledFromMSAAMessage = true;
            }

            return true;
        }

        return false; 
    }
}

#endif