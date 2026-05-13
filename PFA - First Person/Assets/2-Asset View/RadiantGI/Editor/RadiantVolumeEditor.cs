using UnityEngine;
using UnityEditor;
using System.IO;

namespace RadiantGI {

    [CustomEditor(typeof(RadiantVolume))]
    public class RadiantVolumeEditor : Editor {

        SerializedProperty profile, mode, blendDistance;

        RadiantProfile cachedProfile;
        Editor cachedProfileEditor;
        static GUIStyle boxStyle;

        void OnEnable() {
            mode = serializedObject.FindProperty("mode");
            blendDistance = serializedObject.FindProperty("blendDistance");
            profile = serializedObject.FindProperty("profile");
        }

        public override void OnInspectorGUI() {

            if (boxStyle == null) {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(mode);
            EditorGUILayout.PropertyField(blendDistance);
            EditorGUILayout.PropertyField(profile);

            if (profile.objectReferenceValue != null) {
                if (cachedProfile != profile.objectReferenceValue) {
                    cachedProfile = null;
                }
                if (cachedProfile == null) {
                    cachedProfile = (RadiantProfile)profile.objectReferenceValue;
                    cachedProfileEditor = CreateEditor(profile.objectReferenceValue);
                }

                // Drawing the profile editor
                EditorGUILayout.BeginVertical(boxStyle);
                cachedProfileEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            } else {
                EditorGUILayout.HelpBox("Create or assign a Radiant profile.", MessageType.Info);
                if (GUILayout.Button("New Radiant Profile")) {
                    CreateRadiantProfile();
                }
            }


            serializedObject.ApplyModifiedProperties();

        }

        void CreateRadiantProfile() {

            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                path = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(path)) {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }
            RadiantProfile profile = CreateInstance<RadiantProfile>();
            profile.name = "New Radiant GI Profile";
            AssetDatabase.CreateAsset(profile, path + "/" + profile.name + ".asset");
            AssetDatabase.SaveAssets();
            this.profile.objectReferenceValue = profile;
            EditorGUIUtility.PingObject(profile);
        }
    }

    public static class RadiantVolumeEditorExtension {

        [MenuItem("GameObject/Create Other/Radiant GI/Radiant Volume")]
        static void CreateVolume(MenuCommand menuCommand) {
            GameObject volume = new GameObject("Radiant GI Volume", typeof(RadiantVolume));

            GameObjectUtility.SetParentAndAlign(volume, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(volume, "Create Radiant Volume");
            Selection.activeObject = volume;
        }

    }

}

