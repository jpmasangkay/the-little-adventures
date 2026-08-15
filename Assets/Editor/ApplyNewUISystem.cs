using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ApplyNewUISystem
{
    [MenuItem("Tools/Fix All Loading Screens")]
    private static void RunUpgrade()
    {
        
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Village.unity", "Assets/Scenes/BigIsland.unity" };
        
        // Load Assets
        string bgSpritePath = "Assets/Assets/UI/Panels/Wallpaper.png";
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
        
        string pbPrefabPath = "Assets/Assets/InfinityPBR - Magic Pig Games/Progress Bar/Prefabs/Horizontal Progress Bar.prefab";
        GameObject pbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pbPrefabPath);

        if (pbPrefab == null || bgSprite == null)
        {
            Debug.LogError("Could not find the UI assets!");
            return;
        }

        // Save current open scene state
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string startingScene = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            // Nuke the old LoadingCanvas
            GameObject oldCanvasObj = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "LoadingCanvas" && go.scene == scene)
                {
                    oldCanvasObj = go;
                    break;
                }
            }
            if (oldCanvasObj != null) GameObject.DestroyImmediate(oldCanvasObj);

            // Create new LoadingCanvas
            GameObject canvasObj = new GameObject("LoadingCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.layer = LayerMask.NameToLayer("UI");

            // Background (Dark, sleek, matching the reference)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f, 1f); // Dark grey/off-black
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Remove Overlay (since background is now dark)

            // Instantiate Progress Bar (using Object.Instantiate to avoid broken prefab constraints)
            GameObject pbObj = (GameObject)Object.Instantiate(pbPrefab);
            pbObj.transform.SetParent(canvasObj.transform, false);
            pbObj.name = "Horizontal Progress Bar";

            RectTransform pbRect = pbObj.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0.5f);
            pbRect.anchorMax = new Vector2(0.5f, 0.5f);
            pbRect.sizeDelta = new Vector2(800, 100); 
            pbRect.anchoredPosition3D = new Vector3(0, -100, 0); 
            pbObj.transform.localScale = Vector3.one; // FORCE scale to 1 in case it collapses
            pbObj.SetActive(true);

            Slider slider = pbObj.GetComponent<Slider>();
            if (slider == null) slider = pbObj.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.maxValue = 1f;
                slider.value = 0f;
            }

            // Add new Text label above the progress bar
            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "LOADING..."; // Uppercase like reference
            tmpText.fontSize = 48; // Smaller, cleaner
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            // Subtle shadow instead of heavy outline for a cleaner look
            tmpText.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            tmpText.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 2f);
            tmpText.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -2f);
            tmpText.fontSharedMaterial.SetColor("_UnderlayColor", new Color32(0, 0, 0, 128));
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(500, 60);
            textRect.anchoredPosition = new Vector2(0, -30); // Positioned closely above the bar

            canvasObj.SetActive(false);

            // Hook it up to the controller
            if (scene.name == "MainMenu")
            {
                var controllerObj = GameObject.Find("MainMenuController");
                if (controllerObj == null) controllerObj = GameObject.Find("Canvas");
                if (controllerObj != null)
                {
                    var controller = controllerObj.GetComponent("MainMenuController");
                    if (controller != null)
                    {
                        SerializedObject so = new SerializedObject(controller);
                        so.FindProperty("loadingScreenCanvas").objectReferenceValue = canvasObj;
                        if (slider != null) so.FindProperty("loadingBar").objectReferenceValue = slider;
                        so.FindProperty("progressText").objectReferenceValue = tmpText;
                        so.ApplyModifiedProperties();
                    }
                }
            }
            else if (scene.name == "Village" || scene.name == "BigIsland")
            {
                // Find any ScenePortal
                var portals = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
                foreach (var p in portals)
                {
                    if (p.GetType().Name == "ScenePortal")
                    {
                        SerializedObject so = new SerializedObject(p);
                        so.FindProperty("loadingScreenCanvas").objectReferenceValue = canvasObj;
                        if (slider != null) so.FindProperty("loadingBar").objectReferenceValue = slider;
                        so.FindProperty("progressText").objectReferenceValue = tmpText;
                        so.ApplyModifiedProperties();
                    }
                }

                // CRITICAL FIX: Ensure all portals are actually triggers so you can walk into them!
                var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
                foreach(var go in allGameObjects)
                {
                    if (go.name.StartsWith("Portal", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Collider col = go.GetComponent<Collider>();
                        if (col != null)
                        {
                            if (col is MeshCollider mc) mc.convex = true; // Required for triggers on meshes
                            col.isTrigger = true;
                        }
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"🎉 [ApplyNewUISystem] Upgraded loading screen in {scene.name} with InfinityPBR and Cartoon UI Wallpaper!");
        }

        // Return to starting scene
        if (!string.IsNullOrEmpty(startingScene) && startingScene != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(startingScene, OpenSceneMode.Single);
        }
    }
}
