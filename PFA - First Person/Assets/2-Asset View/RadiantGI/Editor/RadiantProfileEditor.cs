using UnityEngine;
using UnityEditor;

namespace RadiantGI
{

    [CustomEditor(typeof(RadiantProfile))]
    public class RadiantProfileEditor : Editor {

        SerializedProperty indirectIntensity, maxIndirectSourceBrightness, indirectDistanceAttenuation, normalMapInfluence, lumaInfluence, virtualEmitters;
        SerializedProperty organicLight, organicLightAnimationSpeed, organicLightDistanceScaling, organicLightNormalsInfluence, organicLightSpread, organicLightThreshold, organicLightTintColor;
        SerializedProperty brightnessThreshold, brightnessMax, specularContribution, sourceBrightness, giWeight, nearCameraAttenuation, saturation, limitToVolumeBounds;
        SerializedProperty nearFieldObscurance, nearFieldObscuranceSpread, nearFieldObscuranceOccluderDistance, nearFieldObscuranceMaxCameraDistance, nearFieldObscuranceTintColor;
        SerializedProperty stencilCheck, stencilValue, stencilCompareFunction;
        SerializedProperty rayCount, rayMaxLength, rayMaxSamples, rayJitter, thickness, rayBinarySearch, rayReuse, rayBounce;
        SerializedProperty fallbackReuseRays, fallbackReflectionProbes, probesIntensity, fallbackReflectiveShadowMap, reflectiveShadowMapIntensity;
        SerializedProperty downsampling, raytracerAccuracy, smoothing;
        SerializedProperty temporalReprojection, temporalResponseSpeed, temporalCameraTranslationResponse, temporalChromaThreshold, temporalDepthRejection;
        SerializedProperty showInEditMode, showInSceneView, debugView, debugDepthMultiplier, compareMode, compareSameSide, comparePanning, compareLineAngle, compareLineWidth;

        void OnEnable() {

            indirectIntensity = serializedObject.FindProperty("indirectIntensity");
            maxIndirectSourceBrightness = serializedObject.FindProperty("indirectMaxSourceBrightness");
            indirectDistanceAttenuation = serializedObject.FindProperty("indirectDistanceAttenuation");
            normalMapInfluence = serializedObject.FindProperty("normalMapInfluence");
            lumaInfluence = serializedObject.FindProperty("lumaInfluence");
            virtualEmitters = serializedObject.FindProperty("virtualEmitters");
            organicLight = serializedObject.FindProperty("organicLight");
            organicLightAnimationSpeed = serializedObject.FindProperty("organicLightAnimationSpeed");
            organicLightDistanceScaling = serializedObject.FindProperty("organicLightDistanceScaling");
            organicLightNormalsInfluence = serializedObject.FindProperty("organicLightNormalsInfluence");
            organicLightSpread = serializedObject.FindProperty("organicLightSpread");
            organicLightThreshold = serializedObject.FindProperty("organicLightThreshold");
            organicLightTintColor = serializedObject.FindProperty("organicLightTintColor");
            nearFieldObscurance = serializedObject.FindProperty("nearFieldObscurance");
            nearFieldObscuranceSpread = serializedObject.FindProperty("nearFieldObscuranceSpread");
            nearFieldObscuranceOccluderDistance = serializedObject.FindProperty("nearFieldObscuranceOccluderDistance");
            nearFieldObscuranceMaxCameraDistance = serializedObject.FindProperty("nearFieldObscuranceMaxCameraDistance");
            nearFieldObscuranceTintColor = serializedObject.FindProperty("nearFieldObscuranceTintColor");
            brightnessThreshold = serializedObject.FindProperty("brightnessThreshold");
            brightnessMax = serializedObject.FindProperty("brightnessMax");
            specularContribution = serializedObject.FindProperty("specularContribution");
            sourceBrightness = serializedObject.FindProperty("sourceBrightness");
            giWeight = serializedObject.FindProperty("giWeight");
            nearCameraAttenuation = serializedObject.FindProperty("nearCameraAttenuation");
            saturation = serializedObject.FindProperty("saturation");
            limitToVolumeBounds = serializedObject.FindProperty("limitToVolumeBounds");
            stencilCheck = serializedObject.FindProperty("stencilCheck");
            stencilValue = serializedObject.FindProperty("stencilValue");
            stencilCompareFunction = serializedObject.FindProperty("stencilCompareFunction");
            rayCount = serializedObject.FindProperty("rayCount");
            rayMaxLength = serializedObject.FindProperty("rayMaxLength");
            rayMaxSamples = serializedObject.FindProperty("rayMaxSamples");
            rayJitter = serializedObject.FindProperty("rayJitter");
            thickness = serializedObject.FindProperty("thickness");
            rayBinarySearch = serializedObject.FindProperty("rayBinarySearch");
            rayReuse = serializedObject.FindProperty("rayReuse");
            rayBounce = serializedObject.FindProperty("rayBounce");
            fallbackReuseRays = serializedObject.FindProperty("fallbackReuseRays");
            fallbackReflectionProbes = serializedObject.FindProperty("fallbackReflectionProbes");
            probesIntensity = serializedObject.FindProperty("probesIntensity");
            fallbackReflectiveShadowMap = serializedObject.FindProperty("fallbackReflectiveShadowMap");
            reflectiveShadowMapIntensity = serializedObject.FindProperty("reflectiveShadowMapIntensity");
            downsampling = serializedObject.FindProperty("downsampling");
            raytracerAccuracy = serializedObject.FindProperty("raytracerAccuracy");
            smoothing = serializedObject.FindProperty("smoothing");
            temporalReprojection = serializedObject.FindProperty("temporalReprojection");
            temporalResponseSpeed = serializedObject.FindProperty("temporalResponseSpeed");
            temporalCameraTranslationResponse = serializedObject.FindProperty("temporalCameraTranslationResponse");
            temporalDepthRejection = serializedObject.FindProperty("temporalDepthRejection");
            temporalChromaThreshold = serializedObject.FindProperty("temporalChromaThreshold");
            showInEditMode = serializedObject.FindProperty("showInEditMode");
            showInSceneView = serializedObject.FindProperty("showInSceneView");
            debugView = serializedObject.FindProperty("debugView");
            debugDepthMultiplier = serializedObject.FindProperty("debugDepthMultiplier");
            compareMode = serializedObject.FindProperty("compareMode");
            compareSameSide = serializedObject.FindProperty("compareSameSide");
            comparePanning = serializedObject.FindProperty("comparePanning");
            compareLineAngle = serializedObject.FindProperty("compareLineAngle");
            compareLineWidth = serializedObject.FindProperty("compareLineWidth");
        }

        public override void OnInspectorGUI() {

            serializedObject.Update();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(indirectIntensity, new GUIContent("Indirect Light Intensity"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(indirectDistanceAttenuation, new GUIContent("Distance Attenuation"));
            EditorGUILayout.PropertyField(rayBounce, new GUIContent("One Extra Bounce"));
            EditorGUILayout.PropertyField(maxIndirectSourceBrightness, new GUIContent("Max Source Brightness"));
            EditorGUILayout.PropertyField(normalMapInfluence);
            EditorGUILayout.PropertyField(lumaInfluence);
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(nearFieldObscurance);
            if (nearFieldObscurance.floatValue > 0f) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nearFieldObscuranceSpread, new GUIContent("Spread"));
                EditorGUILayout.PropertyField(nearFieldObscuranceOccluderDistance, new GUIContent("Occluder Distance"));
                EditorGUILayout.PropertyField(nearFieldObscuranceMaxCameraDistance, new GUIContent("Max Camera Distance"));
                EditorGUILayout.PropertyField(nearFieldObscuranceTintColor, new GUIContent("Tint Color"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(virtualEmitters);
            EditorGUILayout.PropertyField(organicLight);
            if (organicLight.floatValue > 0f) {
                EditorGUI.indentLevel++;
                if (!RadiantGlobalIllumination.isRenderingInDeferred) {
                    EditorGUILayout.HelpBox("Organic Light requires deferred rendering path.", MessageType.Warning);
                }
                EditorGUILayout.PropertyField(organicLightSpread, new GUIContent("Spread"));
                EditorGUILayout.PropertyField(organicLightThreshold, new GUIContent("Threshold"));
                EditorGUILayout.PropertyField(organicLightNormalsInfluence, new GUIContent("Normals Influence"));
                EditorGUILayout.PropertyField(organicLightTintColor, new GUIContent("Tint Color"));
                EditorGUILayout.PropertyField(organicLightAnimationSpeed, new GUIContent("Animation Speed"));
                EditorGUILayout.PropertyField(organicLightDistanceScaling, new GUIContent("Distance Scaling"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rayCount);
            EditorGUILayout.PropertyField(rayMaxLength, new GUIContent("Max Distance"));
            EditorGUILayout.PropertyField(rayMaxSamples, new GUIContent("Max Samples"));
            EditorGUILayout.PropertyField(rayJitter, new GUIContent("Jittering"));
            EditorGUILayout.PropertyField(thickness);
            EditorGUILayout.PropertyField(rayBinarySearch, new GUIContent("Binary Search"));
            EditorGUILayout.PropertyField(smoothing);
            EditorGUILayout.PropertyField(temporalReprojection, new GUIContent("Temporal Filter"));
            if (temporalReprojection.boolValue) {
                EditorGUI.indentLevel++;
                if (temporalReprojection.boolValue && !Application.isPlaying) {
                    EditorGUILayout.HelpBox("Temporal filter only works in play mode.", MessageType.Info);
                }
                EditorGUILayout.PropertyField(temporalResponseSpeed, new GUIContent("Response Speed"));
                EditorGUILayout.PropertyField(temporalChromaThreshold, new GUIContent("Chroma Threshold"));
                EditorGUILayout.PropertyField(temporalCameraTranslationResponse, new GUIContent("Camera Translation Response"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Fallbacks", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fallbackReuseRays, new GUIContent("Reuse Rays"));
            if (fallbackReuseRays.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(rayReuse, new GUIContent("Intensity"));
                if (rayReuse.floatValue > 0) {
                    if (!temporalReprojection.boolValue || !Application.isPlaying) {
                        EditorGUILayout.HelpBox("Reuse Rays works in playmode with Temporal Filter enabled.", MessageType.Info);
                    }
                    EditorGUILayout.PropertyField(temporalDepthRejection, new GUIContent("Depth Rejection"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(fallbackReflectionProbes, new GUIContent("Use Reflection Probes"));
            if (fallbackReflectionProbes.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(probesIntensity);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(fallbackReflectiveShadowMap, new GUIContent("Use Reflective Shadow Map"));
            if (fallbackReflectiveShadowMap.boolValue) {
                EditorGUI.indentLevel++;
                if (!RadiantShadowMap.installed) {
                    EditorGUILayout.HelpBox("Add Radiant Shadow Map script to the main directional light.", MessageType.Warning);
                }
                EditorGUILayout.PropertyField(reflectiveShadowMapIntensity, new GUIContent("Intensity"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(raytracerAccuracy);
            EditorGUILayout.PropertyField(downsampling);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Artistic Controls", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(brightnessThreshold);
            EditorGUILayout.PropertyField(brightnessMax, new GUIContent("Maximum Brightness"));
            EditorGUILayout.PropertyField(specularContribution);
            EditorGUILayout.PropertyField(sourceBrightness);
            EditorGUILayout.PropertyField(giWeight, new GUIContent("GI Weight"));
            EditorGUILayout.PropertyField(saturation);
            EditorGUILayout.PropertyField(nearCameraAttenuation);
            EditorGUILayout.PropertyField(limitToVolumeBounds);
            EditorGUILayout.PropertyField(stencilCheck);
            if (stencilCheck.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(stencilValue, new GUIContent("Value"));
                EditorGUILayout.PropertyField(stencilCompareFunction, new GUIContent("Compare Function"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showInEditMode);
            EditorGUILayout.PropertyField(showInSceneView);
            EditorGUILayout.PropertyField(debugView);
            if ((!temporalReprojection.boolValue || !Application.isPlaying) && (debugView.intValue == (int)DebugView.TemporalAccumulationBuffer)) {
                EditorGUILayout.HelpBox("Temporal filter not in execution. No debug output available.", MessageType.Warning);
            } else if (debugView.intValue == (int)DebugView.ReflectiveShadowMap && !fallbackReflectiveShadowMap.boolValue) {
                EditorGUILayout.HelpBox("Reflective Shadow Map fallback option is not enabled. No debug output available.", MessageType.Warning);
            } else if (debugView.intValue == (int)DebugView.Depth)
            {
                EditorGUILayout.PropertyField(debugDepthMultiplier);
            }
            EditorGUILayout.PropertyField(compareMode);
            if (compareMode.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(compareSameSide, new GUIContent("Same Side"));
                if (compareSameSide.boolValue) {
                    EditorGUILayout.PropertyField(comparePanning, new GUIContent("Panning"));
                } else {
                    EditorGUILayout.PropertyField(compareLineAngle, new GUIContent("Line Angle"));
                    EditorGUILayout.PropertyField(compareLineWidth, new GUIContent("Line Width"));
                }
                EditorGUI.indentLevel--;
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
