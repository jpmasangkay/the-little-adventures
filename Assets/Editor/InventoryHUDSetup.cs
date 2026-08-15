#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool that builds the Inventory + XP / Level HUD in the active scene,
/// adds the Inventory and PlayerStats components to the Player, and wires
/// all UI references automatically.
///
/// Run it from:  Tools ▸ RPG ▸ Setup Inventory & XP HUD
/// </summary>
public class InventoryHUDSetup
{
    [MenuItem("Tools/RPG/Setup Inventory & XP HUD")]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[InventoryHUD] Stop Play Mode before running this tool.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        bool changed = false;

        // ── 1. Find the player ────────────────────────────────────────────────
        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("[InventoryHUD] Could not find Player. Make sure a GameObject with " +
                           "'PlayerMovement' component exists in the scene.");
            return;
        }
        Debug.Log($"[InventoryHUD] Found player: {player.name}");

        // ── 2. Add Inventory component to player ──────────────────────────────
        Inventory inventoryComp = player.GetComponent<Inventory>();
        if (inventoryComp == null)
        {
            inventoryComp = player.AddComponent<Inventory>();
            Debug.Log("[InventoryHUD] Added Inventory component to player.");
            changed = true;
        }

        // ── 3. Add PlayerStats component to player ────────────────────────────
        PlayerStats statsComp = player.GetComponent<PlayerStats>();
        if (statsComp == null)
        {
            statsComp = player.AddComponent<PlayerStats>();
            Debug.Log("[InventoryHUD] Added PlayerStats component to player.");
            changed = true;
        }

        // ── 4. Get or create HUD Canvas ───────────────────────────────────────
        GameObject hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null)
        {
            hudCanvas = new GameObject("HUDCanvas");
            Canvas c = hudCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;
            CanvasScaler cs = hudCanvas.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            hudCanvas.AddComponent<GraphicRaycaster>();
            hudCanvas.layer = LayerMask.NameToLayer("UI");
            Debug.Log("[InventoryHUD] Created new HUDCanvas.");
            changed = true;
        }

        // ── 5. Build Inventory Panel (bottom-left) ────────────────────────────
        // Remove old panel if re-running
        GameObject oldInvPanel = GameObject.Find("InventoryPanel");
        if (oldInvPanel != null) Object.DestroyImmediate(oldInvPanel);

        GameObject invPanel = CreatePanel(hudCanvas.transform, "InventoryPanel",
            anchor: new Vector2(0, 0), pivot: new Vector2(0, 0),
            anchoredPos: new Vector2(20, 20), size: new Vector2(320, 60));

        // Background
        Image invBg = invPanel.AddComponent<Image>();
        invBg.color = new Color(0f, 0f, 0f, 0.55f);
        SetRoundedLook(invPanel);

        // Horizontal layout
        HorizontalLayoutGroup hlg = invPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 8, 8);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Coin slot
        TextMeshProUGUI coinText = CreateItemSlot(invPanel.transform, "CoinSlot", "COIN: 0", new Color(1f, 0.85f, 0.1f));
        // Gem slot
        TextMeshProUGUI gemText  = CreateItemSlot(invPanel.transform, "GemSlot",  "GEM: 0",  new Color(0.3f, 0.8f, 1f));
        // Potion slot
        TextMeshProUGUI potionText = CreateItemSlot(invPanel.transform, "PotionSlot", "POT: 0", new Color(0.4f, 1f, 0.5f));

        SetLayerRecursive(invPanel, LayerMask.NameToLayer("UI"));
        changed = true;

        // ── 6. Build XP / Level Panel (bottom-right) ─────────────────────────
        GameObject oldXpPanel = GameObject.Find("XPPanel");
        if (oldXpPanel != null) Object.DestroyImmediate(oldXpPanel);

        GameObject xpPanel = CreatePanel(hudCanvas.transform, "XPPanel",
            anchor: new Vector2(1, 0), pivot: new Vector2(1, 0),
            anchoredPos: new Vector2(-20, 20), size: new Vector2(280, 60));

        Image xpBg = xpPanel.AddComponent<Image>();
        xpBg.color = new Color(0f, 0f, 0f, 0.55f);

        // Level label (top-left inside panel)
        GameObject lvlLabelObj = new GameObject("LevelText");
        lvlLabelObj.transform.SetParent(xpPanel.transform, false);
        RectTransform lvlRect = lvlLabelObj.AddComponent<RectTransform>();
        lvlRect.anchorMin = new Vector2(0, 0.5f);
        lvlRect.anchorMax = new Vector2(0, 0.5f);
        lvlRect.pivot = new Vector2(0, 0.5f);
        lvlRect.anchoredPosition = new Vector2(10, 0);
        lvlRect.sizeDelta = new Vector2(70, 50);
        TextMeshProUGUI lvlTmp = lvlLabelObj.AddComponent<TextMeshProUGUI>();
        lvlTmp.text = "Lvl 1";
        lvlTmp.fontSize = 18;
        lvlTmp.fontStyle = FontStyles.Bold;
        lvlTmp.color = new Color(1f, 0.9f, 0.3f);
        lvlTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // XP bar container
        GameObject xpBarObj = new GameObject("XPBar");
        xpBarObj.transform.SetParent(xpPanel.transform, false);
        RectTransform xpBarRect = xpBarObj.AddComponent<RectTransform>();
        xpBarRect.anchorMin = new Vector2(0, 0.5f);
        xpBarRect.anchorMax = new Vector2(1, 0.5f);
        xpBarRect.pivot = new Vector2(0.5f, 0.5f);
        xpBarRect.anchoredPosition = new Vector2(40, 0);
        xpBarRect.sizeDelta = new Vector2(-100, 18);

        // XP bar background
        Image xpBgBar = xpBarObj.AddComponent<Image>();
        xpBgBar.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Fill area
        GameObject xpFillArea = new GameObject("Fill Area");
        xpFillArea.transform.SetParent(xpBarObj.transform, false);
        RectTransform faRect = xpFillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.sizeDelta = new Vector2(-4, -4);

        // Fill
        GameObject xpFill = new GameObject("Fill");
        xpFill.transform.SetParent(xpFillArea.transform, false);
        RectTransform fillRect = xpFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image xpFillImg = xpFill.AddComponent<Image>();
        xpFillImg.color = new Color(0.4f, 0.7f, 1f);

        // Slider
        Slider xpSlider = xpBarObj.AddComponent<Slider>();
        xpSlider.fillRect = fillRect;
        xpSlider.maxValue = 100;
        xpSlider.value = 0;
        xpSlider.interactable = false;

        // XP text under bar
        GameObject xpTextObj = new GameObject("XPText");
        xpTextObj.transform.SetParent(xpPanel.transform, false);
        RectTransform xpTxtRect = xpTextObj.AddComponent<RectTransform>();
        xpTxtRect.anchorMin = new Vector2(0.3f, 0);
        xpTxtRect.anchorMax = new Vector2(1, 0.5f);
        xpTxtRect.sizeDelta = Vector2.zero;
        xpTxtRect.anchoredPosition = new Vector2(0, -2);
        TextMeshProUGUI xpTmp = xpTextObj.AddComponent<TextMeshProUGUI>();
        xpTmp.text = "0 / 100 XP";
        xpTmp.fontSize = 11;
        xpTmp.color = new Color(0.7f, 0.85f, 1f);
        xpTmp.alignment = TextAlignmentOptions.Center;

        SetLayerRecursive(xpPanel, LayerMask.NameToLayer("UI"));
        changed = true;

        // ── 6.5. Build Damage Flash Overlay ──────────────────────────────────
        GameObject oldFlash = GameObject.Find("DamageFlashImage");
        if (oldFlash != null) Object.DestroyImmediate(oldFlash);

        GameObject flashObj = new GameObject("DamageFlashImage");
        flashObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1f, 0f, 0f, 0f);
        flashImg.raycastTarget = false;
        flashObj.SetActive(false); // disabled by default
        SetLayerRecursive(flashObj, LayerMask.NameToLayer("UI"));

        // ── 7. Wire Inventory component → UI texts via InventoryHUD script ────
        // We add a lightweight InventoryHUD MonoBehaviour to the canvas to drive the text.
        // (Created below this class.)
        GameObject oldHudDriver = GameObject.Find("InventoryHUDDriver");
        if (oldHudDriver != null) Object.DestroyImmediate(oldHudDriver);

        GameObject hudDriver = new GameObject("InventoryHUDDriver");
        hudDriver.transform.SetParent(hudCanvas.transform, false);
        InventoryHUD hudComp = hudDriver.AddComponent<InventoryHUD>();

        SerializedObject hudSO = new SerializedObject(hudComp);
        hudSO.FindProperty("coinText").objectReferenceValue    = coinText;
        hudSO.FindProperty("gemText").objectReferenceValue     = gemText;
        hudSO.FindProperty("potionText").objectReferenceValue  = potionText;
        hudSO.ApplyModifiedProperties();

        // ── 8. Wire PlayerStats & PlayerHealth → UI ───────────────────────────
        SerializedObject statsSO = new SerializedObject(statsComp);
        statsSO.FindProperty("xpSlider").objectReferenceValue   = xpSlider;
        statsSO.FindProperty("levelText").objectReferenceValue  = lvlTmp;
        statsSO.FindProperty("xpText").objectReferenceValue     = xpTmp;
        statsSO.ApplyModifiedProperties();

        PlayerHealth healthComp = player.GetComponent<PlayerHealth>();
        if (healthComp != null)
        {
            SerializedObject healthSO = new SerializedObject(healthComp);
            healthSO.FindProperty("damageFlashImage").objectReferenceValue = flashImg;
            healthSO.ApplyModifiedProperties();
        }

        Debug.Log("[InventoryHUD] Wired all UI references to PlayerStats, PlayerHealth, and InventoryHUD.");

        // ── Save scene ────────────────────────────────────────────────────────
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("🎉 [InventoryHUD] Done! Inventory + XP HUD built and wired successfully.\n" +
                      "▸ Open the Scene view to see the layout.\n" +
                      "▸ Press Play — coins/gems/potions update automatically when you collect items.\n" +
                      "▸ Kill enemies on BigIsland to gain XP and level up.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject FindPlayer()
    {
        // Try tagged first
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) return p;

        // Fall back to PlayerMovement
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        return pm != null ? pm.gameObject : null;
    }

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    private static TextMeshProUGUI CreateItemSlot(Transform parent, string name, string defaultText, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 85;
        le.flexibleWidth  = 0;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return tmp;
    }

    private static void SetRoundedLook(GameObject go)
    {
        // Purely cosmetic — sets corner rounding if available
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
#endif
