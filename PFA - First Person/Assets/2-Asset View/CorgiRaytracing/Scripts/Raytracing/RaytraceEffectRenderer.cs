using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceEffectRenderer))]
public class RaytraceEffectRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.HelpBox("You can change baseDepthTextureMode here if you are having compatibility issues with PPv2 stack, or other post process effects.", 
            MessageType.Info);
    }
}

#endif

[ExecuteAlways, ImageEffectAllowedInSceneView, DefaultExecutionOrder(-1000), RequireComponent(typeof(Camera))]
public class RaytraceEffectRenderer : MonoBehaviour
{
    public bool ApplyDepthTextureModeOnStartup = true; 
    public bool ApplyDepthTextureModeOnUpdate = true;
    public DepthTextureMode baseDepthTextureMode = DepthTextureMode.Depth;

    [System.NonSerialized] public List<RaytraceEffectBase> RaytraceEffects = new List<RaytraceEffectBase>();
    [System.NonSerialized] public Camera OurCamera;

    private void OnEnable()
    {
        OurCamera = GetComponent<Camera>();

        if(ApplyDepthTextureModeOnStartup)
        {
            OurCamera.depthTextureMode = GetDepthTextureMode();
        }
    }

    public void RegisterEffect(RaytraceEffectBase effect)
    {
        RaytraceEffects.Add(effect);
    }

    public void UnregisterEffect(RaytraceEffectBase effect)
    {
        RaytraceEffects.Remove(effect); 
    }

    private DepthTextureMode GetDepthTextureMode()
    {
        var renderPath = OurCamera.renderingPath;
        var mode = baseDepthTextureMode;

        var count = RaytraceEffects.Count;
        for (var i = 0; i < count; ++i)
        {
            var effect = RaytraceEffects[i];
            mode |= effect.GetDepthTextureMode(renderPath);
        }

        return mode;
    }

    private void Update()
    {
        if(ApplyDepthTextureModeOnUpdate)
        {
            OurCamera.depthTextureMode = GetDepthTextureMode();
        }
    }
}
