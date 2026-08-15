using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindUISetupTool : EditorWindow
{
    [MenuItem("Tools/Generate Keybind UI (Current Scene)")]
    public static void RunSetup()
    {
        Debug.Log("[Keybind Setup] Starting...");
        
        // 1. Find InputActionAsset using AssetDatabase (works in any scene!)
        InputActionAsset inputActions = null;
        string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (inputActions != null && inputActions.FindActionMap("Player") != null)
            {
                break; // Found the right one!
            }
        }

        if (inputActions == null)
        {
            Debug.LogError("Could not find an InputActionAsset with a 'Player' map in the project.");
            return;
        }

        // 2. Find Settings Panel (Targeting the Main Menu settings popup)
        GameObject settingsPanel = null;
        Transform rootParent = null;

        SettingsManager sm = Object.FindAnyObjectByType<SettingsManager>(FindObjectsInactive.Include);
        if (sm != null)
        {
            settingsPanel = sm.gameObject;
            rootParent = sm.transform.parent;
        }
        else
        {
            Debug.LogError("Could not find SettingsManager in the scene! Please open the MainMenu scene first.");
            return;
        }

        // --- Cleanup old broken stuff ---
        Transform oldContainer = settingsPanel.transform.Find("ControlsContainer");
        if (oldContainer != null) Undo.DestroyObjectImmediate(oldContainer.gameObject);

        Transform oldBtn = settingsPanel.transform.Find("OpenControlsButton");
        if (oldBtn != null) Undo.DestroyObjectImmediate(oldBtn.gameObject);

        Transform oldControlsPanel = rootParent.Find("ControlsPanel");
        if (oldControlsPanel != null) Undo.DestroyObjectImmediate(oldControlsPanel.gameObject);
        
        Transform oldSwitcher = rootParent.Find("ControlsPanelSwitcher");
        if (oldSwitcher != null) Undo.DestroyObjectImmediate(oldSwitcher.gameObject);
        // ---------------------------------

        // 3. Create KeybindManager if missing
        KeybindManager kbManager = Object.FindAnyObjectByType<KeybindManager>(FindObjectsInactive.Include);
        if (kbManager == null)
        {
            GameObject kbGo = new GameObject("KeybindManager");
            kbManager = kbGo.AddComponent<KeybindManager>();

            // Create Listening Overlay
            GameObject overlay = new GameObject("ListeningOverlay");
            overlay.transform.SetParent(rootParent, false); // Attach to root Canvas
            RectTransform oRect = overlay.AddComponent<RectTransform>();
            oRect.anchorMin = Vector2.zero; oRect.anchorMax = Vector2.one;
            oRect.sizeDelta = Vector2.zero;
            Image oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0, 0, 0, 0.8f);

            GameObject textGo = new GameObject("ListeningText");
            textGo.transform.SetParent(overlay.transform, false);
            RectTransform tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.5f); tRect.anchorMax = new Vector2(0.5f, 0.5f);
            tRect.sizeDelta = new Vector2(500, 100);
            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Press any key to rebind...\n(Press ESC to cancel)";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = 36;

            kbManager.listeningOverlay = overlay;
            overlay.SetActive(false);
            Undo.RegisterCreatedObjectUndo(kbGo, "Create KeybindManager");
        }

        // 4. Create the new Dedicated Controls Panel popup
        GameObject controlsPanel = new GameObject("ControlsPanel");
        controlsPanel.transform.SetParent(rootParent, false);
        RectTransform cpRect = controlsPanel.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.5f, 0.5f);
        cpRect.anchorMax = new Vector2(0.5f, 0.5f);
        cpRect.pivot = new Vector2(0.5f, 0.5f);
        cpRect.sizeDelta = new Vector2(600, 500); // Nice and big!
        
        Image cpImg = controlsPanel.AddComponent<Image>();
        cpImg.color = new Color(1f, 0.96f, 0.88f, 1f); // Cream color like the other UI

        // Title for Controls Panel
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(controlsPanel.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 50);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CONTROLS";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.3f, 0.1f, 0.1f, 1f);
        titleTmp.fontSize = 42;
        titleTmp.fontStyle = FontStyles.Bold;

        // 5. Add "Edit Controls" button to the EXISTING Settings Panel
        GameObject openBtnGo = new GameObject("OpenControlsButton");
        openBtnGo.transform.SetParent(settingsPanel.transform, false);
        RectTransform obRect = openBtnGo.AddComponent<RectTransform>();
        obRect.anchorMin = new Vector2(0.5f, 0f); obRect.anchorMax = new Vector2(0.5f, 0f);
        obRect.pivot = new Vector2(0.5f, 0f);
        obRect.anchoredPosition = new Vector2(0, 30); // 30px from bottom
        obRect.sizeDelta = new Vector2(250, 60);

        Image obImg = openBtnGo.AddComponent<Image>();
        obImg.color = new Color(0.8f, 0.5f, 0.2f, 1f); // Orange-ish button
        Button openBtn = openBtnGo.AddComponent<Button>();

        GameObject obTxtGo = new GameObject("Text");
        obTxtGo.transform.SetParent(openBtnGo.transform, false);
        RectTransform obTxtRect = obTxtGo.AddComponent<RectTransform>();
        obTxtRect.anchorMin = Vector2.zero; obTxtRect.anchorMax = Vector2.one;
        obTxtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI obTmp = obTxtGo.AddComponent<TextMeshProUGUI>();
        obTmp.text = "EDIT CONTROLS";
        obTmp.alignment = TextAlignmentOptions.Center;
        obTmp.color = Color.white;
        obTmp.fontSize = 24;
        obTmp.fontStyle = FontStyles.Bold;

        // 6. Add "Back" button to Controls Panel
        GameObject backBtnGo = new GameObject("BackButton");
        backBtnGo.transform.SetParent(controlsPanel.transform, false);
        RectTransform bbRect = backBtnGo.AddComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0.3f, 0f); bbRect.anchorMax = new Vector2(0.3f, 0f);
        bbRect.pivot = new Vector2(0.5f, 0f);
        bbRect.anchoredPosition = new Vector2(0, 20);
        bbRect.sizeDelta = new Vector2(200, 50);

        Image bbImg = backBtnGo.AddComponent<Image>();
        bbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        Button backBtn = backBtnGo.AddComponent<Button>();

        GameObject bbTxtGo = new GameObject("Text");
        bbTxtGo.transform.SetParent(backBtnGo.transform, false);
        RectTransform bbTxtRect = bbTxtGo.AddComponent<RectTransform>();
        bbTxtRect.anchorMin = Vector2.zero; bbTxtRect.anchorMax = Vector2.one;
        bbTxtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI bbTmp = bbTxtGo.AddComponent<TextMeshProUGUI>();
        bbTmp.text = "BACK";
        bbTmp.alignment = TextAlignmentOptions.Center;
        bbTmp.color = Color.white;
        bbTmp.fontSize = 24;
        bbTmp.fontStyle = FontStyles.Bold;

        // Add "Reset" button
        GameObject resetBtnGo = new GameObject("ResetButton");
        resetBtnGo.transform.SetParent(controlsPanel.transform, false);
        RectTransform rRect = resetBtnGo.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.7f, 0f); rRect.anchorMax = new Vector2(0.7f, 0f);
        rRect.pivot = new Vector2(0.5f, 0f);
        rRect.anchoredPosition = new Vector2(0, 20);
        rRect.sizeDelta = new Vector2(200, 50);

        Image rImg = resetBtnGo.AddComponent<Image>();
        rImg.color = new Color(0.2f, 0.2f, 0.8f, 1f);
        Button resetBtn = resetBtnGo.AddComponent<Button>();

        GameObject rTxtGo = new GameObject("Text");
        rTxtGo.transform.SetParent(resetBtnGo.transform, false);
        RectTransform rTxtRect = rTxtGo.AddComponent<RectTransform>();
        rTxtRect.anchorMin = Vector2.zero; rTxtRect.anchorMax = Vector2.one;
        rTxtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI rTmp = rTxtGo.AddComponent<TextMeshProUGUI>();
        rTmp.text = "RESET";
        rTmp.alignment = TextAlignmentOptions.Center;
        rTmp.color = Color.white;
        rTmp.fontSize = 24;
        rTmp.fontStyle = FontStyles.Bold;

        KeybindResetButton krb = resetBtnGo.AddComponent<KeybindResetButton>();
        krb.resetButton = resetBtn;
        krb.inputActions = inputActions;

        // 7. Add Switcher Script
        GameObject switcherGo = new GameObject("ControlsPanelSwitcher");
        switcherGo.transform.SetParent(rootParent, false);
        ControlsPanelSwitcher switcher = switcherGo.AddComponent<ControlsPanelSwitcher>();
        switcher.settingsPanel = settingsPanel;
        switcher.controlsPanel = controlsPanel;
        switcher.openControlsButton = openBtn;
        switcher.closeControlsButton = backBtn;

        // 8. Create Scroll View in Controls Panel
        GameObject controlsContainer = new GameObject("ControlsScrollView");
        controlsContainer.transform.SetParent(controlsPanel.transform, false);
        RectTransform cRect = controlsContainer.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.05f, 0.15f); // Leave room for back button
        cRect.anchorMax = new Vector2(0.95f, 0.85f); // Leave room for title
        cRect.offsetMin = Vector2.zero; cRect.offsetMax = Vector2.zero;

        Image bg = controlsContainer.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.1f);

        ScrollRect scroll = controlsContainer.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(controlsContainer.transform, false);
        RectTransform vRect = viewport.AddComponent<RectTransform>();
        vRect.anchorMin = Vector2.zero; vRect.anchorMax = Vector2.one;
        vRect.sizeDelta = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vRect;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contRect = content.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0, 1); contRect.anchorMax = new Vector2(1, 1);
        contRect.pivot = new Vector2(0.5f, 1);
        contRect.sizeDelta = new Vector2(0, 0); // Set by layout
        scroll.content = contRect;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

        // 9. Generate Rows for each Action
        var playerMap = inputActions.FindActionMap("Player");
        if (playerMap != null)
        {
            // Auto-inject Potion action if missing so it can be rebound
            if (playerMap.FindAction("Potion") == null)
            {
                var action = playerMap.AddAction("Potion", type: InputActionType.Button);
                action.AddBinding("<Keyboard>/h");
                EditorUtility.SetDirty(inputActions);
                AssetDatabase.SaveAssets();
                Debug.Log("[Keybind Setup] Added missing 'Potion' action to the Input Action Asset.");
            }

            foreach (var action in playerMap.actions)
            {
                // Find keyboard bindings (skip gamepads or mouse looks for this simple UI)
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite) continue; // Skip composite roots
                    if (binding.path.Contains("Mouse") && !binding.path.Contains("Button")) continue; // Skip mouse look
                    if (binding.path.Contains("Gamepad")) continue; // Skip gamepad for now to keep UI clean

                    string labelName = action.name;
                    if (!string.IsNullOrEmpty(binding.name))
                    {
                        // Capitalize first letter of binding name (e.g. "up" -> "Up")
                        string bName = char.ToUpper(binding.name[0]) + binding.name.Substring(1);
                        labelName = action.name + " (" + bName + ")";
                    }

                    CreateRow(content.transform, action, i, labelName);
                }
            }
        }

        Undo.RegisterCreatedObjectUndo(controlsPanel, "Create Controls Panel");
        Undo.RegisterCreatedObjectUndo(openBtnGo, "Create Open Controls Button");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("🎉 [Keybind Setup] Successfully generated Keybind UI as a Dedicated Popup!");
    }

    private static void CreateRow(Transform parent, InputAction action, int bindingIndex, string label)
    {
        GameObject row = new GameObject("Row_" + label);
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 50;

        // Label
        GameObject txtGo = new GameObject("Label");
        txtGo.transform.SetParent(row.transform, false);
        RectTransform txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0, 0); txtRect.anchorMax = new Vector2(0.5f, 1);
        txtRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmpLabel = txtGo.AddComponent<TextMeshProUGUI>();
        tmpLabel.text = label;
        tmpLabel.alignment = TextAlignmentOptions.Left;
        tmpLabel.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark text on cream bg
        tmpLabel.fontSize = 28;
        tmpLabel.fontStyle = FontStyles.Bold;

        // Button
        GameObject btnGo = new GameObject("Button");
        btnGo.transform.SetParent(row.transform, false);
        RectTransform btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.6f, 0.1f); btnRect.anchorMax = new Vector2(1f, 0.9f);
        btnRect.sizeDelta = Vector2.zero;

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button btn = btnGo.AddComponent<Button>();

        GameObject btnTxtGo = new GameObject("Text");
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnTxtRect = btnTxtGo.AddComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpBtn = btnTxtGo.AddComponent<TextMeshProUGUI>();
        tmpBtn.text = "..."; // Set by KeybindButton on start
        tmpBtn.alignment = TextAlignmentOptions.Center;
        tmpBtn.color = Color.yellow;
        tmpBtn.fontSize = 24;

        // Script
        KeybindButton kbBtn = btnGo.AddComponent<KeybindButton>();
        kbBtn.actionReference = InputActionReference.Create(action);
        kbBtn.bindingIndex = bindingIndex;
        kbBtn.actionNameText = tmpLabel;
        kbBtn.bindText = tmpBtn;
        kbBtn.rebindButton = btn;
    }
}
