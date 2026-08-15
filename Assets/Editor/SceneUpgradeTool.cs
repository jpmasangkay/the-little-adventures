using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class SceneUpgradeTool
{
    [MenuItem("Tools/Upgrade All Scenes (TMP & Input)")]
    public static void UpgradeAllScenes()
    {
        string[] scenes = { 
            "Assets/Scenes/MainMenu.unity", 
            "Assets/Scenes/Village.unity", 
            "Assets/Scenes/BigIsland.unity" 
        };

        // Save current open scene state
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string startingScene = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            int textConverted = 0;
            int inputModulesFixed = 0;
            
            // 1. Upgrade EventSystem to New Input System
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            foreach (var es in eventSystems)
            {
                var standalone = es.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    Object.DestroyImmediate(standalone);
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                    inputModulesFixed++;
                }
            }

            // 2. Convert all legacy UI Text to TextMeshProUGUI
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                textConverted += ConvertTextToTMPRecursively(root);
            }

            if (textConverted > 0 || inputModulesFixed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"🎉 [SceneUpgradeTool] Upgraded {scene.name}! Converted {textConverted} Text to TMP. Fixed {inputModulesFixed} Input Modules.");
            }
            else
            {
                Debug.Log($"✨ [SceneUpgradeTool] {scene.name} is already up to date.");
            }
        }

        // Return to starting scene
        if (!string.IsNullOrEmpty(startingScene) && startingScene != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(startingScene, OpenSceneMode.Single);
        }
        
        Debug.Log("✅ [SceneUpgradeTool] All scenes successfully upgraded! You can now playtest.");
    }

    private static int ConvertTextToTMPRecursively(GameObject obj)
    {
        var textComponents = obj.GetComponentsInChildren<Text>(true);
        int count = 0;
        
        foreach (var oldText in textComponents)
        {
            GameObject textObj = oldText.gameObject;
            
            // Store properties
            string textValue = oldText.text;
            Color colorValue = oldText.color;
            int fontSize = oldText.fontSize;
            FontStyle fontStyle = oldText.fontStyle;
            TextAnchor alignment = oldText.alignment;
            bool raycastTarget = oldText.raycastTarget;
            
            Object.DestroyImmediate(oldText);

            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            
            // Restore properties
            tmpText.text = textValue;
            tmpText.color = colorValue;
            tmpText.fontSize = fontSize;
            tmpText.raycastTarget = raycastTarget;

            // Map alignment
            if (alignment == TextAnchor.UpperLeft) tmpText.alignment = TextAlignmentOptions.TopLeft;
            else if (alignment == TextAnchor.UpperCenter) tmpText.alignment = TextAlignmentOptions.Top;
            else if (alignment == TextAnchor.UpperRight) tmpText.alignment = TextAlignmentOptions.TopRight;
            else if (alignment == TextAnchor.MiddleLeft) tmpText.alignment = TextAlignmentOptions.Left;
            else if (alignment == TextAnchor.MiddleCenter) tmpText.alignment = TextAlignmentOptions.Center;
            else if (alignment == TextAnchor.MiddleRight) tmpText.alignment = TextAlignmentOptions.Right;
            else if (alignment == TextAnchor.LowerLeft) tmpText.alignment = TextAlignmentOptions.BottomLeft;
            else if (alignment == TextAnchor.LowerCenter) tmpText.alignment = TextAlignmentOptions.Bottom;
            else if (alignment == TextAnchor.LowerRight) tmpText.alignment = TextAlignmentOptions.BottomRight;

            // Map font style
            if (fontStyle == FontStyle.Bold) tmpText.fontStyle = FontStyles.Bold;
            else if (fontStyle == FontStyle.Italic) tmpText.fontStyle = FontStyles.Italic;
            else if (fontStyle == FontStyle.BoldAndItalic) tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            count++;
        }
        
        return count;
    }
}
