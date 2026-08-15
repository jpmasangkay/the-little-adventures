using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AutoSetupUI : EditorWindow
{
    [MenuItem("Tools/1-Click Setup UI Overhaul (All Scenes)")]
    public static void SetupEverything()
    {
        Debug.Log("Starting Automatic UI Overhaul across ALL scenes...");

        string[] scenesToProcess = {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Village.unity",
            "Assets/Scenes/BigIsland.unity"
        };
        
        string startingScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in scenesToProcess)
        {
            if (System.IO.File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"<color=cyan>Processing Scene: {scenePath}</color>");
                
                SetupLoadingScreen();
                FixGlobalUIAndKeybinds();
                StyleInventoryUI();
                StyleMainMenuUI();
                StylePauseMenuUI();
                
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
        }
        
        // Return to the original scene
        if (!string.IsNullOrEmpty(startingScenePath) && System.IO.File.Exists(startingScenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(startingScenePath);
        }

        Debug.Log("<color=green><b>Success!</b> UI Overhaul is fully complete across all scenes in your project.</color>");
    }

    private static void SetupLoadingScreen()
    {
        // Check if one already exists
        if (Object.FindAnyObjectByType<LoadingScreenManager>() != null)
        {
            Debug.Log("LoadingScreenManager already exists in scene. Skipping creation.");
            return;
        }

        // 1. Create Manager Object
        GameObject managerObj = new GameObject("LoadingScreenManager");
        LoadingScreenManager manager = managerObj.AddComponent<LoadingScreenManager>();

        // 2. Create Canvas
        GameObject canvasObj = new GameObject("LoadingScreenCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Render on top of everything
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 3. Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 4. Loading Bar (Slider)
        GameObject sliderObj = new GameObject("LoadingBar");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.1f, 0.1f);
        sliderRect.anchorMax = new Vector2(0.9f, 0.15f);
        sliderRect.sizeDelta = Vector2.zero;
        
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        
        slider.fillRect = fillRect;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // 5. Loading Text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "LOADING... 0%";
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 36;
        tmpText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.15f);
        textRect.anchorMax = new Vector2(0.9f, 0.25f);
        textRect.sizeDelta = Vector2.zero;

        // Assign to manager
        manager.loadingScreenCanvas = canvasObj;
        manager.loadingBar = slider;
        manager.progressText = tmpText;

        // Disable canvas by default
        canvasObj.SetActive(false);

        Undo.RegisterCreatedObjectUndo(managerObj, "Create Loading Screen");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Loading Screen Canvas");
        
        Debug.Log("Created Loading Screen setup.");
    }

    private static void FixGlobalUIAndKeybinds()
    {
        // Fix the keybinds panel being stuck open (the bug in BigIsland)
        KeybindManager kbManager = Object.FindAnyObjectByType<KeybindManager>(FindObjectsInactive.Include);
        if (kbManager != null)
        {
            // Try to find the root UI panel for controls
            Transform t = kbManager.transform;
            while (t != null && t.GetComponent<Canvas>() == null)
            {
                if (t.name.ToLower().Contains("control") || t.name.ToLower().Contains("keybind") || t.name.ToLower().Contains("panel"))
                {
                    Undo.RecordObject(t.gameObject, "Disable Keybinds Panel");
                    t.gameObject.SetActive(false);
                    Debug.Log($"Disabled {t.name} to fix stuck UI issue.");
                    break;
                }
                t = t.parent;
            }
        }

        // Add GlobalUIManager to the main UI canvas
        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            Canvas rootCanvas = pauseMenu.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.GetComponent<GlobalUIManager>() == null)
            {
                Undo.AddComponent<GlobalUIManager>(rootCanvas.gameObject);
                Debug.Log($"Added GlobalUIManager to {rootCanvas.name}");
            }
        }
    }

    private static void StyleInventoryUI()
    {
        InventoryMenu invMenu = Object.FindAnyObjectByType<InventoryMenu>(FindObjectsInactive.Include);
        if (invMenu == null || invMenu.inventoryPanel == null)
        {
            Debug.Log("Inventory not found. Generating it first using InventoryUISetupTool...");
            InventoryUISetupTool.RunSetup();
            
            invMenu = Object.FindAnyObjectByType<InventoryMenu>(FindObjectsInactive.Include);
            if (invMenu == null || invMenu.inventoryPanel == null)
            {
                Debug.LogWarning("Could not find InventoryMenu or its inventoryPanel to style even after generating.");
                return;
            }
        }

        // Load Wood UI Panel sprite
        string panelSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Wood UI/Wood UI Panels/Wood Panel UI.png";
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(panelSpritePath);

        // Load Wood UI Button sprite
        string btnSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Wood UI/Wood UI Buttons/Wooden Button Blank.png";
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(btnSpritePath);

        if (panelSprite != null)
        {
            Image bgImage = invMenu.inventoryPanel.GetComponent<Image>();
            if (bgImage != null)
            {
                Undo.RecordObject(bgImage, "Style Inventory Background");
                bgImage.sprite = panelSprite;
                bgImage.type = Image.Type.Simple; // Some sprites aren't sliced, Simple looks better if so
                bgImage.color = Color.white;
            }
            
            // Destroy the ugly auto-generated borders so we can actually see the wood panel
            Transform border = invMenu.inventoryPanel.transform.Find("Border");
            if (border != null) Undo.DestroyObjectImmediate(border.gameObject);
            
            Debug.Log("Applied Wood Panel UI sprite to Inventory and removed old borders.");
        }

        if (btnSprite != null)
        {
            Button[] buttons = invMenu.inventoryPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    Undo.RecordObject(btnImg, "Style Inventory Button");
                    btnImg.sprite = btnSprite;
                    btnImg.type = Image.Type.Simple;
                    
                    // Fix button size if it's the Use Button so it doesn't look squished
                    if (btn.name == "UseButton")
                    {
                        RectTransform rt = btn.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(160, 80); // Make it a bit bigger and more proportionate
                    }
                }
            }
            Debug.Log($"Applied Wood Button sprite to {buttons.Length} inventory buttons.");
        }

        // Fix the POTIONS text wrapping issue
        Transform potionsRow = invMenu.inventoryPanel.transform.Find("ItemsList/Row_Potions");
        if (potionsRow != null)
        {
            Transform label = potionsRow.Find("Label");
            if (label != null)
            {
                TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Fix Potions Text Wrap");
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.fontSize = 36; // slightly smaller so it fits with the big button
                }
            }
        }

        // Apply Icons
        ApplyIconToRow(invMenu.inventoryPanel.transform, "ItemsList/Row_Coins/Icon", "Assets/Assets/Button UI by BlastOffProductions/Misc Game Icons/Gold Icon.PNG");
        ApplyIconToRow(invMenu.inventoryPanel.transform, "ItemsList/Row_Gems/Icon", "Assets/Assets/Button UI by BlastOffProductions/Misc Game Icons/Diamond Icon.png");
        ApplyIconToRow(invMenu.inventoryPanel.transform, "ItemsList/Row_Potions/Icon", "Assets/Assets/Button UI by BlastOffProductions/Misc Game Icons/Health Potion Icon.png");
    }

    private static void ApplyIconToRow(Transform parent, string path, string spritePath)
    {
        Transform iconTrans = parent.Find(path);
        if (iconTrans != null)
        {
            Image img = iconTrans.GetComponent<Image>();
            if (img != null)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    Undo.RecordObject(img, "Apply Item Icon");
                    img.sprite = sprite;
                    img.color = Color.white; // Reset from placeholder color
                }
                else
                {
                    Debug.LogWarning("Could not find icon sprite at: " + spritePath);
                }
            }
        }
    }

    private static void StyleMainMenuUI()
    {
        MainMenuController mainMenu = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            return;
        }

        string bgSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Flat-Raised UI Buttons/Flat Yellow Panel.png";
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
        
        string btnSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Metalic Buttons/Blue Metal Button Blank.png";
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(btnSpritePath);

        if (mainMenu.mainPanel != null && bgSprite != null)
        {
            Image img = mainMenu.mainPanel.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "Style MainMenu Background"); img.sprite = bgSprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        }

        if (mainMenu.settingsPanel != null && bgSprite != null)
        {
            Image img = mainMenu.settingsPanel.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "Style Settings Background"); img.sprite = bgSprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        }

        Button[] buttons = mainMenu.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null && btnSprite != null)
            {
                Undo.RecordObject(img, "Style MainMenu Button");
                img.sprite = btnSprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
        }
        
        Debug.Log("Main Menu UI Overhaul Complete.");
    }

    private static void StylePauseMenuUI()
    {
        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu == null) return;
        
        string bgSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Flat-Raised UI Buttons/Flat Yellow Panel.png";
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
        
        string btnSpritePath = "Assets/Assets/Button UI by BlastOffProductions/Metalic Buttons/Blue Metal Button Blank.png";
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(btnSpritePath);
        
        if (pauseMenu.pauseCanvas != null && bgSprite != null)
        {
            Image img = pauseMenu.pauseCanvas.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "Style PauseMenu Background"); img.sprite = bgSprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        }

        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null && btnSprite != null)
            {
                Undo.RecordObject(img, "Style PauseMenu Button");
                img.sprite = btnSprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
        }
        
        Debug.Log("Pause Menu UI Overhaul Complete.");
    }
}
