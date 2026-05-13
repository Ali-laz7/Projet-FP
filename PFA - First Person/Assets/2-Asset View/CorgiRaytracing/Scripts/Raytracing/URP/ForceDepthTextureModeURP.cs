using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(-1000), RequireComponent(typeof(Camera))]
public class ForceDepthTextureModeURP : MonoBehaviour
{
    public bool ApplyDepthTextureModeOnStartup = true;
    public bool ApplyDepthTextureModeOnUpdate = true;
    public DepthTextureMode baseDepthTextureMode = DepthTextureMode.Depth;

    [System.NonSerialized] public Camera OurCamera;

    private void OnEnable()
    {
        OurCamera = GetComponent<Camera>();

        if (ApplyDepthTextureModeOnStartup)
        {
            OurCamera.depthTextureMode = baseDepthTextureMode;
        }
    }

    private void Update()
    {
        if (ApplyDepthTextureModeOnUpdate)
        {
            OurCamera.depthTextureMode = baseDepthTextureMode;
        }
    }
}
