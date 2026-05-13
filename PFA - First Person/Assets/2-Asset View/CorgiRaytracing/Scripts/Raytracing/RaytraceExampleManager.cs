using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RaytraceExampleManager))]
public class RaytraceExampleManagerEditor : RaytraceEffectBaseEditor
{

}
#endif

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RaytraceExampleManager : RaytraceEffectBase
{
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

    protected override string GetRaygenShaderName(RenderingPath renderPath)
    {
        return "MyRaygenShader";
    }

    protected override string GetRaytracingShaderPassName()
    {
        return "RaytraceExamplePass";
    }
}
