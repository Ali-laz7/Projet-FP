
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RaytraceToolsWindow : EditorWindow
{
    private static string ConfirmMessage = "Are you sure?? Be sure to backup your project with source control, before you confirm!";

    [MenuItem("Corgi/Raytrace/Tools")]
    public static void InitializeWindow()
    {
        EditorWindow.GetWindow<RaytraceToolsWindow>();

    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tools");
        
        if(GUILayout.Button("Scene: Disable Raytracing on all Renderers"))
        {
            if(EditorUtility.DisplayDialog("Disable Raytracing on all Scene Renderers?", ConfirmMessage, "YES!", "No.."))
            {
                OnButton_SceneRaytraceDisableAll();
            }
        }

        if (GUILayout.Button("Scene: Enable Raytracing on all recommended Renderers"))
        {
            if (EditorUtility.DisplayDialog("Enable Raytracing on all recommended Scene Renderers?", ConfirmMessage, "YES!", "No.."))
            {
                OnButton_SceneEnableRaytracingRecommended();
            }
        }

        if (GUILayout.Button("Prefab: Disable Raytracing on all Renderers"))
        {
            if (EditorUtility.DisplayDialog("Disable Raytracing on all Prefab Renderers?", ConfirmMessage, "YES!", "No.."))
            {
                OnButton_PrefabsRaytracingDisableAll();
            }
        }

        if (GUILayout.Button("Prefabs: Enable Raytracing on all recommended Renderers"))
        {
            if (EditorUtility.DisplayDialog("Enable Raytracing on all recommended Prefab Renderers?", ConfirmMessage, "YES!", "No.."))
            {
                OnButton_PrefabsEnableRaytracingRecommended();
            }
        }
    }

    private static void OnButton_SceneRaytraceDisableAll()
    {
#if UNITY_2020_2_OR_NEWER
        var all_renderers = FindObjectsOfType<Renderer>(true);
#else
        var all_renderers = FindObjectsOfType<Renderer>();
#endif
        RaytraceDisableAll(all_renderers);
    }

    private static Renderer[] FindAllPrefabs()
    {
        var valid_renderers = new List<Renderer>();

        EditorUtility.DisplayProgressBar("Quering prefabs..", "Loading..", 0f);

        var queryPathResults = AssetDatabase.FindAssets("t:Object");
        var queryPathResultsCount = queryPathResults.Length;
        for (var i = 0; i < queryPathResultsCount; ++i)
        {
            var cancel = EditorUtility.DisplayCancelableProgressBar("Quering prefabs", $"{i} / {queryPathResultsCount}", (float) i / queryPathResultsCount);
            if(cancel)
            {
                valid_renderers.Clear();
                break;
            }

            var guid = queryPathResults[i];
            var queryPath = AssetDatabase.GUIDToAssetPath(guid);

            var queryObject = AssetDatabase.LoadAssetAtPath<GameObject>(queryPath);

            if (queryObject == null)
            {
                continue;
            }

            var childRenderers = queryObject.GetComponentsInChildren<Renderer>(true);
            var childRendererCount = childRenderers.Length;

            for (var cr = 0; cr < childRendererCount; ++cr)
            {
                var childRenderer = childRenderers[cr];
                valid_renderers.Add(childRenderer);
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"queryPathResultsCount: {queryPathResultsCount}, valid_renderers.Count: {valid_renderers.Count}"); 

        var all_renderers = valid_renderers.ToArray();
        return all_renderers;
    }

    private static void RaytraceDisableAll(Renderer[] all_renderers)
    {
        Undo.RecordObjects(all_renderers, "SceneRaytraceDisableAll");

        var all_renderers_count = all_renderers.Length;
        for (var i = 0; i < all_renderers_count; ++i)
        {
            var renderer = all_renderers[i];
            renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(renderer.gameObject);
        }
    }

    private static void OnButton_SceneEnableRaytracingRecommended()
    {
#if UNITY_2020_2_OR_NEWER
        var all_renderers = FindObjectsOfType<Renderer>(true);
#else
        var all_renderers = FindObjectsOfType<Renderer>();
#endif

        EnableRaytracingRecommended(all_renderers);
    }
    
    private static void OnButton_PrefabsEnableRaytracingRecommended()
    {
        AssetDatabase.StartAssetEditing();

        try
        {
            var all_renderers = FindAllPrefabs();
            EnableRaytracingRecommended(all_renderers);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e); 
        }

        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets(); 
    }

    private static void OnButton_PrefabsRaytracingDisableAll()
    {
        AssetDatabase.StartAssetEditing();

        try
        {
            var all_renderers = FindAllPrefabs();
            RaytraceDisableAll(all_renderers);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e); 
        }

        AssetDatabase.StopAssetEditing(); 

        AssetDatabase.SaveAssets();
    }

    private static void EnableRaytracingRecommended(Renderer[] all_renderers)
    {
        Undo.RecordObjects(all_renderers, "SceneEnableRaytracingRecommended");

        AssetDatabase.StartAssetEditing();
        EditorUtility.DisplayProgressBar("EnableRaytracingRecommended", "Starting up.", 0f);

        try
        {
            var all_renderers_count = all_renderers.Length;
            for (var i = 0; i < all_renderers_count; ++i)
            {
                var cancel = EditorUtility.DisplayCancelableProgressBar("EnableRaytracingRecommended", $"{i}/{all_renderers_count}", (float)i / all_renderers_count);
                if(cancel)
                {
                    Undo.PerformUndo(); 
                    break; 
                }

                var renderer = all_renderers[i];
                renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;

                // as of Unity 2020.2.1f1,
                // particle systems cannot be ray traced against, yet :( 
                if (renderer as ParticleSystemRenderer != null)
                {
                    continue;
                }

                // as of Unity 2020.2.2f1,
                // trail renderers cannot be ray traced against, yet :( 
                if (renderer as TrailRenderer != null)
                {
                    continue;
                }

                // if we are a part of a LODGroup, do not re-enable unless we are the cheapest LOD
                var parentLodGroup = renderer.GetComponentInParent<LODGroup>();
                if (parentLodGroup != null && parentLodGroup.lodCount > 0)
                {
                    var lods = parentLodGroup.GetLODs();
                    var lod = lods[parentLodGroup.lodCount - 1];

                    var rendererIsLastInLastLod = false;

                    var lodRendererCount = lod.renderers.Length;
                    for (var lr = 0; lr < lodRendererCount; ++lr)
                    {
                        var lodRenderer = lod.renderers[lr];
                        if (lodRenderer == renderer)
                        {
                            rendererIsLastInLastLod = true;
                            break;
                        }
                    }

                    if (!rendererIsLastInLastLod)
                    {
                        continue;
                    }
                }

                // if the object is marked as statically batched, keep RayTracingMode to static too
                if (renderer.gameObject.isStatic)
                {
                    renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Static;
                }
                // if the renderer is skinned, it needs to be marked as dynamic geometry 
                else if (renderer as SkinnedMeshRenderer != null)
                {
                    renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry;
                }
                // otherwise, assume it needs to be dynamic 
                else
                {
                    renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
                }

                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(renderer.gameObject);
            }

        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        AssetDatabase.StopAssetEditing();
        EditorUtility.ClearProgressBar();
    }

    [MenuItem("GameObject/Raytracing/Enable RTX (Static)", false, 0)]
    private static void ContextMenuRaytracingEnableStatic()
    {
        var objects = Selection.objects;
        foreach (var context in objects)
        {
            var gameObject = (GameObject)context;
            if (gameObject == null) continue;

            var transform = gameObject.transform;
            if (transform == null) continue;

            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; ++i)
            {
                var renderer = renderers[i];

                if (renderer as ParticleSystemRenderer != null)
                {
                    continue;
                }

                if (renderer as TrailRenderer != null)
                {
                    continue;
                }

                var parentLODGroup = renderer.GetComponentInParent<LODGroup>();
                if (parentLODGroup != null)
                {
                    continue;
                }

                renderer.rayTracingMode = RayTracingMode.Static;

                UnityEditor.EditorUtility.SetDirty(renderer);
                UnityEditor.EditorUtility.SetDirty(renderer.gameObject);
            }
        }
    }
    [MenuItem("GameObject/Raytracing/Enable RTX (Dynamic Transform)", false, 0)]
    private static void ContextMenuRaytracingEnableDynamicTransform()
    {
        var objects = Selection.objects;
        foreach (var context in objects)
        {
            var gameObject = (GameObject)context;
            if (gameObject == null) continue;

            var transform = gameObject.transform;
            if (transform == null) continue;

            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; ++i)
            {
                var renderer = renderers[i];

                if (renderer as ParticleSystemRenderer != null)
                {
                    continue;
                }

                if (renderer as TrailRenderer != null)
                {
                    continue;
                }

                var parentLODGroup = renderer.GetComponentInParent<LODGroup>();
                if (parentLODGroup != null)
                {
                    continue;
                }

                renderer.rayTracingMode = RayTracingMode.DynamicTransform;

                UnityEditor.EditorUtility.SetDirty(renderer);
                UnityEditor.EditorUtility.SetDirty(renderer.gameObject);
            }
        }
    }

    [MenuItem("GameObject/Raytracing/Disable RTX", false, 0)]
    private static void ContextMenuRaytracingDisable()
    {
        var objects = Selection.objects;
        foreach (var context in objects)
        {
            var gameObject = (GameObject)context;
            if (gameObject == null) continue;

            var transform = gameObject.transform;
            if (transform == null) continue;

            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; ++i)
            {
                var renderer = renderers[i];
                renderer.rayTracingMode = RayTracingMode.Off;

                UnityEditor.EditorUtility.SetDirty(renderer);
                UnityEditor.EditorUtility.SetDirty(renderer.gameObject);
            }
        }
    }

}

#endif