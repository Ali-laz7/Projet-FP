using UnityEngine;
using UnityEngine.Rendering;

namespace RadiantGI {

    public enum DebugView {
        None,
        Albedo,
        Normals,
        Specular,
        Depth,
        Raycast = 20,
        DownscaledHalf = 30,
        DownscaledQuarter = 40,
        ReflectiveShadowMap = 41,
        UpscaleToHalf = 50,
        TemporalAccumulationBuffer = 60,
        FinalGI = 70
    }

    [CreateAssetMenu(menuName = "Radiant Profile", fileName = "RadiantProfile", order = 336)]
    public class RadiantProfile : ScriptableObject {

        [Tooltip("Intensity of the indirect lighting.")]
        public float indirectIntensity;

        [Tooltip("Distance attenuation applied to indirect lighting. Reduces indirect intensity by square of distance.")]
        [Range(0, 1)]
        public float indirectDistanceAttenuation;

        [Tooltip("Maximum brightness of indirect source.")]
        public float indirectMaxSourceBrightness = 8;

        [Tooltip("Determines how much influence has the surface normal map when receiving indirect lighting.")]
        [Range(0, 1)]
        public float normalMapInfluence = 1f;

        [Tooltip("Add one ray bounce.")]
        public bool rayBounce;

        [Tooltip("Only in forward rendering mode: uses pixel luma to enhance results by adding variety to the effect based on the perceptual brigthness. Set this value to 0 to disable this feature.")]
        public float lumaInfluence;

        [Tooltip("Intensity of the near field obscurance effect. Darkens surfaces occluded by other nearby surfaces.")]
        public float nearFieldObscurance;

        [Tooltip("Spread or radius of the near field obscurance effect")]
        [Range(0.01f, 1f)]
        public float nearFieldObscuranceSpread = 0.2f;

        [Tooltip("Maximum distance from camera of Near Field Obscurance effect")]
        public float nearFieldObscuranceMaxCameraDistance = 125f;

        [Tooltip("Distance threshold of the occluder")]
        [Range(0, 1f)]
        public float nearFieldObscuranceOccluderDistance = 0.825f;

        [Tooltip("Tint color of Near Field Obscurance effect")]
        [ColorUsage(showAlpha: false)]
        public Color nearFieldObscuranceTintColor = Color.black;

        [Tooltip("Enable user-defined light emitters in the scene.")]
        public bool virtualEmitters;

        [Tooltip("Intensity of organic light. This option injects artifical/procedural light variations into g-buffers to product a more natural and interesting lit environment. This added lighting is also used as source for indirect lighting.")]
        [Range(0, 1)]
        public float organicLight;

        [Tooltip("Threshold of organic light noise calculation")]
        [Range(0, 1)]
        public float organicLightThreshold = 0.5f;

        [Tooltip("Organic light spread")]
        [Range(0.9f, 1f)]
        public float organicLightSpread = 0.98f;

        [Tooltip("Organic light normal influence preserves normal map effect on textures")]
        [Range(0, 1)]
        public float organicLightNormalsInfluence = 0.95f;

        [Tooltip("Organic light tint color")]
        [ColorUsage(showAlpha: false)]
        public Color organicLightTintColor = Color.white;

        [Tooltip("Animation speed")]
        public Vector3 organicLightAnimationSpeed;

        [Tooltip("Reduces organic light pattern repetition at the distance")]
        public bool organicLightDistanceScaling;
        [Tooltip("Number of rays per pixel")]
        [Range(1, 4)]
        public int rayCount = 1;

        [Tooltip("Max ray length. Increasing this value may also require increasing the 'Max Samples' value to avoid losing quality.")]
        public float rayMaxLength = 8;

        [Tooltip("Max samples taken during raymarch.")]
        public int rayMaxSamples = 32;

        [Tooltip("Jitter adds a random offset to the ray direction to reduce banding. Useful when using low sample count.")]
        public float rayJitter;

        [Tooltip("The assumed thickness for any geometry. Used to determine if ray crosses a surface.")]
        public float thickness = 1f;

        [Tooltip("Improves raymarch accuracy by using binary search.")]
        public bool rayBinarySearch = true;

        [Tooltip("In case a ray miss a target, reuse rays from previous frames.")]
        public bool fallbackReuseRays;

        [Tooltip("If a ray misses a target, reuse result from history buffer. This value is the intensity of the previous color in case the ray misses the target.")]
        [Range(0, 1)]
        public float rayReuse;

        [Tooltip("In case a ray miss a target, use nearby probes if they're available.")]
        public bool fallbackReflectionProbes;

        [Tooltip("Custom global probe intensity multiplier. Note that each probe has also an intensity property.")]
        public float probesIntensity = 1f;

        [Tooltip("In case a ray miss a target, use reflective shadow map data from the main directional light. You need to add the ReflectiveShadowMap script to the directional light to use this feature.")]
        public bool fallbackReflectiveShadowMap;

        [Range(0, 1)]
        public float reflectiveShadowMapIntensity = 0.8f;

        [Tooltip("Reduces resolution of all GI stages improving performance")]
        [Range(1, 4)]
        public float downsampling = 1;

        [Tooltip("Raytracing accuracy. Reducing this value will shrink the depth buffer used during raytracing, improving performance in exchange of accuracy.")]
        [Range(1, 8)]
        public int raytracerAccuracy = 8;

        [Tooltip("Extra blur passes")]
        [Range(0, 4)]
        public int smoothing = 3;

        [Tooltip("Uses motion vectors to blend into a history buffer to reduce flickering. Only applies in play mode.")]
        public bool temporalReprojection = true;

        [Tooltip("Reaction speed to screen changes. Higher values reduces ghosting but also the smoothing.")]
        public float temporalResponseSpeed = 12;

        [Tooltip("Reaction speed to camera position change. Higher values reduces ghosting when camera moves.")]
        public float temporalCameraTranslationResponse = 100;

        [Tooltip("Difference in depth with current frame to discard history buffer when reusing rays.")]
        public float temporalDepthRejection = 1f;

        [Tooltip("Allowed difference in color between history and current GI buffers.")]
        [Range(0, 2f)]
        public float temporalChromaThreshold = 0.2f;

        [Tooltip("Renders the effect also in edit mode (when not in play-mode).")]
        public bool showInEditMode = true;

        [Tooltip("Renders the effect also in Scene View.")]
        public bool showInSceneView = true;

        [Tooltip("Computes GI emitted by objects with a minimum luminosity.")]
        public float brightnessThreshold;

        [Tooltip("Maximum GI brightness.")]
        public float brightnessMax = 8f;

        [Range(0, 1)]
        [Tooltip("Amount of GI which adds to specular surfaces. Reduce this value to avoid overexposition of shiny materials.")]
        public float specularContribution = 0.75f;

        [Range(0, 2)]
        [Tooltip("Brightness of the original image. You may reduce this value to make GI more prominent.")]
        public float sourceBrightness = 1f;

        [Tooltip("Increases final GI contribution vs source color pixel. Increase this value to reduce the intensity of the source pixel color based on the received GI amount, making the applied GI more apparent.")]
        public float giWeight;

        [Tooltip("Attenuates GI brightness from nearby surfaces.")]
        public float nearCameraAttenuation;

        [Tooltip("Adjusted color saturation for the computed GI.")]
        [Range(0, 2)]
        public float saturation = 1f;

        [Tooltip("Applies GI only inside the post processing volume (use only if the volume is local)")]
        public bool limitToVolumeBounds;

        [Tooltip("Enables stencil check during GI composition. This option let you exclude GI over certain objects that also use stencil buffer.")]
        public bool stencilCheck;

        public int stencilValue;

        public CompareFunction stencilCompareFunction = CompareFunction.NotEqual;

        public DebugView debugView = DebugView.None;

        [Tooltip("Depth values multiplier for the depth debug view")]
        public float debugDepthMultiplier = 10;

        public bool compareMode;

        public bool compareSameSide;

        [Range(0, 0.5f)]
        public float comparePanning = 0.25f;

        [Range(-Mathf.PI, Mathf.PI)]
        public float compareLineAngle = 1.4f;

        [Range(0.0001f, 0.05f)]
        public float compareLineWidth = 0.002f;


        public bool IsActive() => indirectIntensity > 0 || compareMode;

        void OnValidate() {
            indirectIntensity = Mathf.Max(0, indirectIntensity);
            indirectMaxSourceBrightness = Mathf.Max(0, indirectMaxSourceBrightness);
            temporalResponseSpeed = Mathf.Max(0, temporalResponseSpeed);
            temporalDepthRejection = Mathf.Max(0, temporalDepthRejection);
            rayMaxLength = Mathf.Max(0.1f, rayMaxLength);
            rayMaxSamples = Mathf.Max(2, rayMaxSamples);
            rayJitter = Mathf.Max(0, rayJitter);
            lumaInfluence = Mathf.Max(0, lumaInfluence);
            thickness = Mathf.Max(0.1f, thickness);
            brightnessThreshold = Mathf.Max(0, brightnessThreshold);
            brightnessMax = Mathf.Max(0, brightnessMax);
            probesIntensity = Mathf.Max(0, probesIntensity);
            stencilValue = Mathf.Max(0, stencilValue);
            nearCameraAttenuation = Mathf.Max(0, nearCameraAttenuation);
            nearFieldObscurance = Mathf.Max(0, nearFieldObscurance);
            nearFieldObscuranceMaxCameraDistance = Mathf.Max(0, nearFieldObscuranceMaxCameraDistance);
            giWeight = Mathf.Max(0, giWeight);
        }

        public void Apply(RadiantGlobalIllumination radiant) {
            radiant.indirectIntensity = indirectIntensity;
            radiant.indirectDistanceAttenuation = indirectDistanceAttenuation;
            radiant.indirectMaxSourceBrightness = indirectMaxSourceBrightness;
            radiant.normalMapInfluence = normalMapInfluence;
            radiant.rayBounce = rayBounce;
            radiant.lumaInfluence = lumaInfluence;
            radiant.nearFieldObscurance = nearFieldObscurance;
            radiant.nearFieldObscuranceSpread = nearFieldObscuranceSpread;
            radiant.nearFieldObscuranceMaxCameraDistance = nearFieldObscuranceMaxCameraDistance;
            radiant.nearFieldObscuranceOccluderDistance = nearFieldObscuranceOccluderDistance;
            radiant.nearFieldObscuranceTintColor = nearFieldObscuranceTintColor;
            radiant.virtualEmitters = virtualEmitters;
            radiant.organicLight = organicLight;
            radiant.organicLightAnimationSpeed = organicLightAnimationSpeed;
            radiant.organicLightDistanceScaling = organicLightDistanceScaling;
            radiant.organicLightNormalsInfluence = organicLightNormalsInfluence;
            radiant.organicLightSpread = organicLightSpread;
            radiant.organicLightThreshold = organicLightThreshold;
            radiant.organicLightTintColor = organicLightTintColor;
            radiant.rayCount = rayCount;
            radiant.rayMaxLength = rayMaxLength;
            radiant.rayMaxSamples = rayMaxSamples;
            radiant.rayJitter = rayJitter;
            radiant.thickness = thickness;
            radiant.rayBinarySearch = rayBinarySearch;
            radiant.smoothing = smoothing;
            radiant.temporalReprojection = temporalReprojection;
            radiant.temporalResponseSpeed = temporalResponseSpeed;
            radiant.temporalCameraTranslationResponse = temporalCameraTranslationResponse;
            radiant.temporalDepthRejection = temporalDepthRejection;
            radiant.temporalChromaThreshold = temporalChromaThreshold;
            radiant.fallbackReuseRays = fallbackReuseRays;
            radiant.rayReuse = rayReuse;
            radiant.fallbackReflectionProbes = fallbackReflectionProbes;
            radiant.probesIntensity = probesIntensity;
            radiant.fallbackReflectiveShadowMap = fallbackReflectiveShadowMap;
            radiant.reflectiveShadowMapIntensity = reflectiveShadowMapIntensity;
            radiant.downsampling = downsampling;
            radiant.raytracerAccuracy = raytracerAccuracy;
            radiant.brightnessThreshold = brightnessThreshold;
            radiant.brightnessMax = brightnessMax;
            radiant.specularContribution = specularContribution;
            radiant.sourceBrightness = sourceBrightness;
            radiant.giWeight = giWeight;
            radiant.nearCameraAttenuation = nearCameraAttenuation;
            radiant.saturation = saturation;
            radiant.limitToVolumeBounds = limitToVolumeBounds;
            radiant.stencilCheck = stencilCheck;
            radiant.stencilValue = stencilValue;
            radiant.stencilCompareFunction = stencilCompareFunction;
            radiant.debugView = debugView;
            radiant.debugDepthMultiplier = debugDepthMultiplier;
            radiant.showInEditMode = showInEditMode;
            radiant.showInSceneView = showInSceneView;
            radiant.compareMode = compareMode;
            radiant.compareSameSide = compareSameSide;
            radiant.comparePanning = comparePanning;
            radiant.compareLineAngle = compareLineAngle;
            radiant.compareLineWidth = compareLineWidth;
        }

    }

}

