using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUISetupTool : EditorWindow
{
    [MenuItem("Tools/Generate Inventory UI (Current Scene)")]
    public static void RunSetup()
    {
        Debug.Log("[Inventory Setup] Starting...");

        // 1. Ensure 'Inventory' action exists in InputActionAsset
        InputActionAsset inputActions = null;
        string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (inputActions != null && inputActions.FindActionMap("Player") != null)
                break;
        }

        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null && playerMap.FindAction("Inventory") == null)
            {
                var action = playerMap.AddAction("Inventory", type: InputActionType.Button);
                action.AddBinding("<Keyboard>/i");
                action.AddBinding("<Keyboard>/tab");
                EditorUtility.SetDirty(inputActions);
                AssetDatabase.SaveAssets();
                Debug.Log("[Inventory Setup] Added 'Inventory' action (I / Tab) to Input Asset.");
            }
        }

        // 2. Create Canvas
        GameObject canvasGo = GameObject.Find("InventoryCanvas");
        if (canvasGo != null)
        {
            Undo.DestroyObjectImmediate(canvasGo);
        }

        canvasGo = new GameObject("InventoryCanvas");
        Canvas c = canvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 90; // Just below pause menu but above HUD
        
        CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        
        canvasGo.AddComponent<GraphicRaycaster>();

        // 3. Create Main Panel
        GameObject panelGo = new GameObject("InventoryPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform pRect = panelGo.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0.5f); pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.pivot = new Vector2(0.5f, 0.5f);
        pRect.sizeDelta = new Vector2(600, 700);

        Image pImg = panelGo.AddComponent<Image>();
        pImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-ish background

        // Border
        GameObject borderGo = new GameObject("Border");
        borderGo.transform.SetParent(panelGo.transform, false);
        RectTransform bRect = borderGo.AddComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = new Vector2(-10, -10);
        Image bImg = borderGo.AddComponent<Image>();
        bImg.color = new Color(0.8f, 0.7f, 0.2f, 1f); // Gold border
        
        GameObject innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(borderGo.transform, false);
        RectTransform iRect = innerGo.AddComponent<RectTransform>();
        iRect.anchorMin = Vector2.zero; iRect.anchorMax = Vector2.one;
        iRect.sizeDelta = new Vector2(-10, -10);
        Image iImg = innerGo.AddComponent<Image>();
        iImg.color = new Color(0.15f, 0.15f, 0.2f, 1f); // Darker inner

        // 4. Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(0, 80);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "INVENTORY";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.9f, 0.8f, 0.3f, 1f); // Gold text
        titleTmp.fontSize = 60;
        titleTmp.fontStyle = FontStyles.Bold;

        // 5. Items Layout Group
        GameObject itemsGo = new GameObject("ItemsList");
        itemsGo.transform.SetParent(panelGo.transform, false);
        RectTransform itemsRect = itemsGo.AddComponent<RectTransform>();
        itemsRect.anchorMin = new Vector2(0.1f, 0.2f); itemsRect.anchorMax = new Vector2(0.9f, 0.8f);
        itemsRect.offsetMin = Vector2.zero; itemsRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = itemsGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.spacing = 30;
        vlg.padding = new RectOffset(20, 20, 20, 20);

        // 6. Create Item Rows
        TextMeshProUGUI coinsTmp = CreateItemRow(itemsGo.transform, "Coins", new Color(1f, 0.8f, 0f, 1f));
        TextMeshProUGUI gemsTmp = CreateItemRow(itemsGo.transform, "Gems", new Color(0f, 0.8f, 1f, 1f));
        var (potTmp, useBtn) = CreatePotionRow(itemsGo.transform);

        // 7. Close Button
        GameObject closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(panelGo.transform, false);
        RectTransform cRect = closeGo.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(1, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(1, 1);
        cRect.anchoredPosition = new Vector2(-15, -15);
        cRect.sizeDelta = new Vector2(60, 60);

        Image cImg = closeGo.AddComponent<Image>();
        cImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        Button closeBtn = closeGo.AddComponent<Button>();

        GameObject cTxtGo = new GameObject("Text");
        cTxtGo.transform.SetParent(closeGo.transform, false);
        RectTransform cTxtRect = cTxtGo.AddComponent<RectTransform>();
        cTxtRect.anchorMin = Vector2.zero; cTxtRect.anchorMax = Vector2.one;
        cTxtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI cTmp = cTxtGo.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;
        cTmp.fontSize = 36;
        cTmp.fontStyle = FontStyles.Bold;

        // 8. Attach InventoryMenu script
        InventoryMenu menu = canvasGo.AddComponent<InventoryMenu>();
        menu.inventoryPanel = panelGo;
        menu.coinsText = coinsTmp;
        menu.gemsText = gemsTmp;
        menu.potionsText = potTmp;
        menu.usePotionButton = useBtn;
        menu.closeButton = closeBtn;

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Inventory UI");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("🎉 [Inventory Setup] Successfully generated Inventory UI!");
    }

    private static TextMeshProUGUI CreateItemRow(Transform parent, string name, Color iconColor)
    {
        GameObject row = new GameObject("Row_" + name);
        row.transform.SetParent(parent, false);
        RectTransform rRect = row.AddComponent<RectTransform>();
        rRect.sizeDelta = new Vector2(0, 100);

        // Icon placeholder
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(row.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f); iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(20, 0);
        iconRect.sizeDelta = new Vector2(80, 80);
        Image img = icon.AddComponent<Image>();
        img.color = iconColor;

        // Label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(row.transform, false);
        RectTransform lRect = label.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0, 0); lRect.anchorMax = new Vector2(1, 1);
        lRect.offsetMin = new Vector2(120, 0); lRect.offsetMax = new Vector2(-150, 0);
        TextMeshProUGUI lTmp = label.AddComponent<TextMeshProUGUI>();
        lTmp.text = name.ToUpper();
        lTmp.alignment = TextAlignmentOptions.Left;
        lTmp.color = Color.white;
        lTmp.fontSize = 40;

        // Value text
        GameObject val = new GameObject("Value");
        val.transform.SetParent(row.transform, false);
        RectTransform vRect = val.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(1, 0); vRect.anchorMax = new Vector2(1, 1);
        vRect.pivot = new Vector2(1, 0.5f);
        vRect.offsetMin = new Vector2(-150, 0); vRect.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI vTmp = val.AddComponent<TextMeshProUGUI>();
        vTmp.text = "0";
        vTmp.alignment = TextAlignmentOptions.Right;
        vTmp.color = Color.yellow;
        vTmp.fontSize = 48;
        vTmp.fontStyle = FontStyles.Bold;

        return vTmp;
    }

    private static (TextMeshProUGUI, Button) CreatePotionRow(Transform parent)
    {
        // Potion row is special because it has a USE button
        GameObject row = new GameObject("Row_Potions");
        row.transform.SetParent(parent, false);
        RectTransform rRect = row.AddComponent<RectTransform>();
        rRect.sizeDelta = new Vector2(0, 100);

        // Icon placeholder
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(row.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f); iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(20, 0);
        iconRect.sizeDelta = new Vector2(80, 80);
        Image img = icon.AddComponent<Image>();
        img.color = new Color(0.8f, 0.2f, 0.4f, 1f); // Red-ish pink for potion

        // Label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(row.transform, false);
        RectTransform lRect = label.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0, 0); lRect.anchorMax = new Vector2(1, 1);
        lRect.offsetMin = new Vector2(120, 0); lRect.offsetMax = new Vector2(-250, 0); // leave room for button
        TextMeshProUGUI lTmp = label.AddComponent<TextMeshProUGUI>();
        lTmp.text = "POTIONS";
        lTmp.alignment = TextAlignmentOptions.Left;
        lTmp.color = Color.white;
        lTmp.fontSize = 40;

        // Value text
        GameObject val = new GameObject("Value");
        val.transform.SetParent(row.transform, false);
        RectTransform vRect = val.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(1, 0); vRect.anchorMax = new Vector2(1, 1);
        vRect.pivot = new Vector2(1, 0.5f);
        vRect.offsetMin = new Vector2(-250, 0); vRect.offsetMax = new Vector2(-150, 0);
        TextMeshProUGUI vTmp = val.AddComponent<TextMeshProUGUI>();
        vTmp.text = "0";
        vTmp.alignment = TextAlignmentOptions.Right;
        vTmp.color = Color.yellow;
        vTmp.fontSize = 48;
        vTmp.fontStyle = FontStyles.Bold;

        // Use Button
        GameObject useBtnGo = new GameObject("UseButton");
        useBtnGo.transform.SetParent(row.transform, false);
        RectTransform uRect = useBtnGo.AddComponent<RectTransform>();
        uRect.anchorMin = new Vector2(1, 0.5f); uRect.anchorMax = new Vector2(1, 0.5f);
        uRect.pivot = new Vector2(1, 0.5f);
        uRect.anchoredPosition = new Vector2(-20, 0);
        uRect.sizeDelta = new Vector2(120, 60);

        Image uImg = useBtnGo.AddComponent<Image>();
        uImg.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Green button
        Button useBtn = useBtnGo.AddComponent<Button>();

        GameObject uTxtGo = new GameObject("Text");
        uTxtGo.transform.SetParent(useBtnGo.transform, false);
        RectTransform uTxtRect = uTxtGo.AddComponent<RectTransform>();
        uTxtRect.anchorMin = Vector2.zero; uTxtRect.anchorMax = Vector2.one;
        uTxtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI uTmp = uTxtGo.AddComponent<TextMeshProUGUI>();
        uTmp.text = "USE";
        uTmp.alignment = TextAlignmentOptions.Center;
        uTmp.color = Color.white;
        uTmp.fontSize = 28;
        uTmp.fontStyle = FontStyles.Bold;

        return (vTmp, useBtn);
    }
}
