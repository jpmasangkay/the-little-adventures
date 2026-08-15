using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class MinimapGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Minimaps for All Scenes")]
    public static void GenerateAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string currentScene = EditorSceneManager.GetActiveScene().path;

        string[] scenes = {
            "Assets/Scenes/Village.unity",
            "Assets/Scenes/BigIsland.unity"
        };

        if (!Directory.Exists("Assets/Resources/Minimaps"))
        {
            Directory.CreateDirectory("Assets/Resources/Minimaps");
        }

        foreach (string scenePath in scenes)
        {
            if (File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
                GenerateForCurrentScene();
            }
            else
            {
                Debug.LogWarning("Scene not found: " + scenePath);
            }
        }

        // Return to original scene
        if (!string.IsNullOrEmpty(currentScene) && File.Exists(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene);
        }

        AssetDatabase.Refresh();
        Debug.Log("✅ Minimaps generated successfully! The CircularMinimap will now automatically load them.");
    }

    private static void GenerateForCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        
        // Find bounds of all mesh renderers and terrains
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        
        foreach (Renderer r in renderers)
        {
            // Skip UI, Particles, Trails, etc. which can have huge bounds.
            // Terrain is NOT a Renderer subclass, so we check via GetComponent.
            bool isMesh    = r is MeshRenderer;
            bool isTerrain = r.GetComponent<Terrain>() != null;
            if (!isMesh && !isTerrain) continue;
            
            if (first) 
            { 
                bounds = r.bounds; 
                first = false; 
            }
            else 
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (first) 
        {
            Debug.LogWarning("No MeshRenderers or Terrains found in " + scene.name);
            return; // No bounds found
        }

        // We want a square minimap to make rotation mapping simple
        float size = Mathf.Max(bounds.size.x, bounds.size.z);
        // Add a 10% padding so things aren't right on the edge
        size *= 1.1f; 
        Vector3 center = bounds.center;

        // Create camera
        GameObject camGO = new GameObject("TempMinimapCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size / 2f;
        cam.transform.position = new Vector3(center.x, bounds.max.y + 100f, center.z);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        // A nice dark green base color for empty spots
        cam.backgroundColor = new Color(0.12f, 0.18f, 0.12f, 1f); 

        // Render to texture at a much higher resolution
        int res = 4096;
        RenderTexture rt = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        // Clean up
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(camGO);
        DestroyImmediate(rt);

        // Save PNG
        byte[] bytes = tex.EncodeToPNG();
        DestroyImmediate(tex);

        string path = "Assets/Resources/Minimaps/" + scene.name + ".png";
        File.WriteAllBytes(path, bytes);
        
        // Force Unity to recognize the new file immediately
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        
        // Configure Unity's import settings so it doesn't downscale or compress the high-res map!
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        
        // Save bounds config so the minimap script knows the scale and offset
        string configPath = "Assets/Resources/Minimaps/" + scene.name + "_bounds.txt";
        string configData = $"{size},{size},{center.x},{center.z}";
        File.WriteAllText(configPath, configData);
        
        Debug.Log("Generated minimap for " + scene.name + " (Size: " + size + ")");
    }
}
