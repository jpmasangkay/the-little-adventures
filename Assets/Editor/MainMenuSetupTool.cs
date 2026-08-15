using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class MainMenuSetupTool
{
    static MainMenuSetupTool()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        if (SessionState.GetBool("MainMenuSetupToolRun", false))
            return;
        SessionState.SetBool("MainMenuSetupToolRun", true);

        string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        // Save current scene state before changing
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Open Main Menu
        Scene scene = EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        bool changed = false;

        // 1. Find or create Loading Canvas
        GameObject loadingCanvasObj = GameObject.Find("LoadingCanvas");
        if (loadingCanvasObj == null)
        {
            loadingCanvasObj = new GameObject("LoadingCanvas");
            Canvas canvas = loadingCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            loadingCanvasObj.AddComponent<GraphicRaycaster>();
            loadingCanvasObj.layer = LayerMask.NameToLayer("UI");

            // Ensure it sorts on top of everything
            canvas.sortingOrder = 100;

            // Background Image
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(loadingCanvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = Color.black;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Loading Text
            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(loadingCanvasObj.transform, false);
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Loading...";
            tmpText.fontSize = 72;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;

            // Hide by default
            loadingCanvasObj.SetActive(false);
            changed = true;
            Debug.Log("✅ [MainMenuSetupTool] Created Loading Screen UI.");
        }

        // 2. Wire it up to MainMenuController
        var controllerObj = GameObject.Find("MainMenuController");
        if (controllerObj == null) controllerObj = GameObject.Find("Canvas"); // Fallback

        if (controllerObj != null)
        {
            var controller = controllerObj.GetComponent("MainMenuController");
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                
                so.FindProperty("loadingScreenCanvas").objectReferenceValue = loadingCanvasObj;
                
                Transform textTransform = loadingCanvasObj.transform.Find("LoadingText");
                if (textTransform != null)
                {
                    so.FindProperty("progressText").objectReferenceValue = textTransform.GetComponent<TextMeshProUGUI>();
                }
                
                so.ApplyModifiedProperties();
                changed = true;
                Debug.Log("✅ [MainMenuSetupTool] Wired up Loading Screen to MainMenuController.");
            }
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("🎉 [MainMenuSetupTool] Successfully updated Main Menu!");
        }
    }
}
