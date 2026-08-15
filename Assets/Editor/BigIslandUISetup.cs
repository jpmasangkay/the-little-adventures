using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class BigIslandUISetup
{
    [MenuItem("Tools/Setup HUD (Current Scene)")]
    public static void RunSetup()
    {
        Debug.Log("[HUD Setup] Starting setup on current scene...");
        if (Application.isPlaying)
        {
            Debug.LogWarning("Cannot run UI Setup in Play Mode. Please stop the game first.");
            return;
        }
        
        string hbPrefabPath = "Assets/Assets/Ilumisoft/Health System/Prefabs/Healthbar.prefab";
        GameObject hbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hbPrefabPath);

        if (hbPrefab == null)
        {
            Debug.LogError("[HUD Setup] Could not find the Ilumisoft Healthbar prefab at: " + hbPrefabPath);
            return;
        }
        Debug.Log("[HUD Setup] Found Healthbar prefab.");

        var scene = EditorSceneManager.GetActiveScene();
        bool changed = false;

        // 1. Ensure Player has PlayerHealth component
        GameObject playerObj = GameObject.Find("MaleCharacterPBR");
        if (playerObj != null)
        {
            var healthScript = playerObj.GetComponent("PlayerHealth");
            if (healthScript == null)
            {
                var type = System.Type.GetType("PlayerHealth, Assembly-CSharp");
                if (type != null)
                {
                    healthScript = playerObj.AddComponent(type);
                    changed = true;
                }
            }

            // 2. Ensure HUD Canvas
            GameObject hudCanvasObj = GameObject.Find("HUDCanvas");
            if (hudCanvasObj == null)
            {
                hudCanvasObj = new GameObject("HUDCanvas");
                Canvas canvas = hudCanvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                CanvasScaler scaler = hudCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                hudCanvasObj.AddComponent<GraphicRaycaster>();
                hudCanvasObj.layer = LayerMask.NameToLayer("UI");
                hudCanvasObj.SetActive(true);
                changed = true;
            }

            // 3. Build a foolproof native Unity Healthbar to bypass prefab nesting bugs
            Transform hbTransform = null;
            GameObject existingHb = GameObject.Find("Healthbar_Custom");
            if (existingHb != null) Object.DestroyImmediate(existingHb); // Nuke old one

            Debug.Log("[HUD Setup] Building native Unity Healthbar...");
            GameObject customHb = new GameObject("Healthbar_Custom");
            customHb.transform.SetParent(hudCanvasObj.transform, false);
            hbTransform = customHb.transform;
            
            RectTransform hbRect = customHb.AddComponent<RectTransform>();
            hbRect.anchorMin = new Vector2(0, 1);
            hbRect.anchorMax = new Vector2(0, 1);
            hbRect.pivot = new Vector2(0, 1);
            hbRect.anchoredPosition3D = new Vector3(20, -20, 0);
            hbRect.sizeDelta = new Vector2(400, 40); // Sleek bar

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(customHb.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(customHb.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-10, -10);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.9f, 0.1f, 0.2f, 1f); // Nice red
            
            Slider slider = customHb.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.maxValue = 100f;
            slider.value = 100f;
            slider.interactable = false;

            // Extract styles from Ilumisoft prefab to maintain the requested look
            if (hbPrefab != null)
            {
                Image[] ilumImages = hbPrefab.GetComponentsInChildren<Image>(true);
                foreach (var img in ilumImages) 
                { 
                    string n = img.name.ToLower();
                    if (n.Contains("bg") || n.Contains("back")) bgImg.sprite = img.sprite;
                    if (n.Contains("fill") || n.Contains("health")) { fillImg.sprite = img.sprite; fillImg.color = img.color; }
                }
            }

            // Force visibility
            int uiLayer = LayerMask.NameToLayer("UI");
            foreach (Transform t in customHb.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = uiLayer;
            }
            changed = true;

            // Destroy the Ilumisoft script so it doesn't throw NullReferenceExceptions
            if (hbTransform != null)
            {
                var oldScript = hbTransform.GetComponent("Ilumisoft.HealthSystem.UI.Healthbar");
                if (oldScript != null)
                {
                    Object.DestroyImmediate(oldScript);
                    changed = true;
                }
            }

            // 4. Wire up Healthbar to PlayerHealth
            if (healthScript != null && hbTransform != null)
            {
                SerializedObject so = new SerializedObject(healthScript);
                
                Slider foundSlider = hbTransform.GetComponent<Slider>();
                if (foundSlider == null) foundSlider = hbTransform.GetComponentInChildren<Slider>();
                
                if (foundSlider != null)
                {
                    so.FindProperty("healthSlider").objectReferenceValue = foundSlider;
                }
                else
                {
                    Image fillImage = hbTransform.GetComponent<Image>();
                    if (fillImage == null) fillImage = hbTransform.GetComponentInChildren<Image>();
                    if (fillImage != null) so.FindProperty("healthFill").objectReferenceValue = fillImage;
                }
                so.ApplyModifiedProperties();
                changed = true;
            }
        }

        else
        {
            Debug.LogWarning("[HUD Setup] Player 'MaleCharacterPBR' not found in scene! Cannot setup HUD.");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("🎉 [HUD Setup] Successfully added Healthbar to the current scene!");
        }
        else
        {
            Debug.LogWarning("[HUD Setup] No changes were made.");
        }
    }
}
