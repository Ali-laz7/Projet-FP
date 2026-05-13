using UnityEngine;
using UnityEditor;
using static RadiantGI.RadiantGlobalIllumination;

namespace RadiantGI {

    
    [CustomEditor(typeof(RadiantGlobalIllumination))]
    public class RadiantGlobalIlluminationEditor : Editor {

        SerializedProperty includeForward, volumeMask, normalsQuality;

        RadiantGlobalIllumination radiant;
        Camera cam;

        private void OnEnable() {
            includeForward = serializedObject.FindProperty("includeForward");
            volumeMask = serializedObject.FindProperty("volumeMask");
            normalsQuality = serializedObject.FindProperty("normalsQuality");
            radiant = (RadiantGlobalIllumination)target;
            cam = radiant.GetComponent<Camera>();
        }

        public override void OnInspectorGUI() {

            serializedObject.Update();

            EditorGUILayout.PropertyField(volumeMask);
            if (cam != null) {
                if (cam.actualRenderingPath == UnityEngine.RenderingPath.Forward) {
                    EditorGUILayout.PropertyField(normalsQuality);
                }
                if (cam.actualRenderingPath == UnityEngine.RenderingPath.DeferredShading) {
                    EditorGUILayout.PropertyField(includeForward);
                    EditorGUILayout.HelpBox("Enable only if your scene uses forward rendering materials that render in the opaque queue.", MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
