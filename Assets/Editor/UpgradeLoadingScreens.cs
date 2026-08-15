using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class UpgradeLoadingScreens
{
    static UpgradeLoadingScreens()
    {
        EditorApplication.delayCall += RunUpgrade;
    }

    private static void RunUpgrade()
    {
        if (SessionState.GetBool("UpgradeLoadingScreensRun", false)) return;
        SessionState.SetBool("UpgradeLoadingScreensRun", true);
        
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Village.unity" };
        string pbPrefabPath = "Assets/Assets/UI/Prefabs/Progress Bars/Type 1/ProgressBar Blue.prefab";
        GameObject pbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pbPrefabPath);

        if (pbPrefab == null)
        {
            Debug.LogError("Could not find the ProgressBar prefab from Cartoon UI!");
            return;
        }

        // Save current open scene state
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string startingScene = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            GameObject canvasObj = GameObject.Find("LoadingCanvas");
            if (canvasObj == null) continue;

            // Check if we already added a progress bar
            if (canvasObj.transform.Find("ProgressBar Blue") != null) continue;

            // Delete old text
            Transform oldText = canvasObj.transform.Find("LoadingText");
            if (oldText != null) GameObject.DestroyImmediate(oldText.gameObject);

            // Instantiate Progress Bar
            GameObject pbObj = (GameObject)PrefabUtility.InstantiatePrefab(pbPrefab);
            pbObj.transform.SetParent(canvasObj.transform, false);
            pbObj.name = "ProgressBar Blue";

            RectTransform pbRect = pbObj.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0.5f);
            pbRect.anchorMax = new Vector2(0.5f, 0.5f);
            pbRect.anchoredPosition = new Vector2(0, -50); // Move down slightly

            Slider slider = pbObj.GetComponent<Slider>();
            if (slider != null)
            {
                slider.maxValue = 1f;
                slider.value = 0f;
            }

            // Add new Text label above the progress bar
            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Loading...";
            tmpText.fontSize = 64;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(600, 100);
            textRect.anchoredPosition = new Vector2(0, 50); // Move up slightly

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
                        so.FindProperty("loadingBar").objectReferenceValue = slider;
                        so.FindProperty("progressText").objectReferenceValue = tmpText;
                        so.ApplyModifiedProperties();
                    }
                }
            }
            else if (scene.name == "Village")
            {
                var portalObj = GameObject.Find("Portal01");
                if (portalObj != null)
                {
                    var portal = portalObj.GetComponent("ScenePortal");
                    if (portal != null)
                    {
                        SerializedObject so = new SerializedObject(portal);
                        so.FindProperty("loadingBar").objectReferenceValue = slider;
                        so.FindProperty("progressText").objectReferenceValue = tmpText;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"🎉 [UpgradeLoadingScreens] Upgraded loading screen in {scene.name} with Cartoon UI!");
        }

        // Return to starting scene
        if (!string.IsNullOrEmpty(startingScene) && startingScene != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(startingScene, OpenSceneMode.Single);
        }
    }
}
