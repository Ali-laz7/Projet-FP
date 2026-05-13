using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CorgiDemoUI : MonoBehaviour
{
    [Header("Camera Settings")]
    public RaytraceReflectionsManager ReflectionsManager;
    public RaytraceShadowsManager ShadowsManager;
    public RaytraceEffectVolumetricLighting VolumetricShadowsManager;
    public AutomaticallyRotate RotateCameraManager;
    public AutomaticallyBob BobCameraManager;

    [Header("UI Settings")]
    public Toggle Toggle_Reflections;
    public Toggle Toggle_Shadows;
    public Toggle Toggle_VolumetricShadows;
    public Slider Slider_Reflections;
    public Slider Slider_ReflectionsBounces;
    public Slider Slider_Shadows;
    public Slider Slider_VolumetricShadows;
    public Dropdown Dropdown_Reflections;
    public Dropdown Dropdown_Shadows;

    public Toggle Toggle_BounceCamera;
    public Toggle Toggle_RotateCamera;

    public Text Text_FPS;
    public GameObject RTXNotSupported;
    public Text Text_RTXNotSupported;

    private void OnEnable()
    {
        Toggle_Reflections.SetIsOnWithoutNotify(ReflectionsManager.enabled);
        Toggle_Shadows.SetIsOnWithoutNotify(ShadowsManager.enabled);
        Toggle_VolumetricShadows.SetIsOnWithoutNotify(VolumetricShadowsManager.enabled);

        Slider_Reflections.SetValueWithoutNotify(ReflectionsManager.TextureScaleReciprocal);
        Slider_ReflectionsBounces.SetValueWithoutNotify((int) ReflectionsManager.Bounces);
        Slider_Shadows.SetValueWithoutNotify(ShadowsManager.TextureScaleReciprocal);
        Slider_VolumetricShadows.SetValueWithoutNotify(VolumetricShadowsManager.TextureScaleReciprocal);

        Dropdown_Reflections.SetValueWithoutNotify((int) ReflectionsManager.Roughness);
        Dropdown_Shadows.SetValueWithoutNotify((int) ShadowsManager.ShadowQuality);

        Toggle_BounceCamera.SetIsOnWithoutNotify(BobCameraManager.enabled);
        Toggle_RotateCamera.SetIsOnWithoutNotify(RotateCameraManager.enabled);
    }

    private void Start()
    {
        RTXNotSupported.gameObject.SetActive(!SystemInfo.supportsRayTracing);

        Text_RTXNotSupported.text = 
            $"Raytracing is not supported on this device!" +
            $"\n{SystemInfo.graphicsDeviceName}" +
            $"\n{SystemInfo.graphicsDeviceType}"; 
    }

    private void Update()
    {
        Text_FPS.text = $"{1f / Time.smoothDeltaTime:N0} fps";
    }

    public void OnToggle_BounceCamera(Toggle toggle)
    {
        BobCameraManager.enabled = toggle.isOn;
    }

    public void OnToggle_RotateCamera(Toggle toggle)
    {
        RotateCameraManager.enabled = toggle.isOn;
    }

    public void OnToggle_Reflections(Toggle toggle)
    {
        ReflectionsManager.enabled = toggle.isOn;
    }

    public void OnToggle_Shadows(Toggle toggle)
    {
        ShadowsManager.enabled = toggle.isOn;
    }

    public void OnToggle_VolumetricShadows(Toggle toggle)
    {
        VolumetricShadowsManager.enabled = toggle.isOn;
    }

    public void OnSlider_Reflections(Slider slider)
    {
        ReflectionsManager.TextureScaleReciprocal = (int) slider.value;
    }

    public void OnSlider_ReflectionsBounces(Slider slider)
    {
        ReflectionsManager.Bounces = (RaytraceQuality) slider.value;
    }

    public void OnSlider_Shadows(Slider slider)
    {
        ShadowsManager.TextureScaleReciprocal = (int)slider.value;
    }

    public void OnSlider_VolumetricShadows(Slider slider)
    {
        VolumetricShadowsManager.TextureScaleReciprocal = (int)slider.value;
    }

    public void OnDropdown_Reflections(Dropdown dropdown)
    {
        ReflectionsManager.Roughness = (RaytraceQuality) dropdown.value;
    }

    public void OnDropdown_Shadows(Dropdown dropdown)
    {
        ShadowsManager.ShadowQuality = (RaytraceQuality)dropdown.value;
    }

}
