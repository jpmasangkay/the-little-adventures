using UnityEngine;
using TMPro;

/// <summary>
/// Listens to the Inventory singleton and keeps the HUD coin/gem/potion 
/// text labels up to date. Placed on HUDCanvas by the InventoryHUDSetup 
/// editor tool — no manual wiring needed.
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    [Header("Item Text Labels")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI gemText;
    public TextMeshProUGUI potionText;

    private void OnEnable()
    {
        // Subscribe when scene loads or component enables
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;
        
        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Start()
    {
        // Re-subscribe after scene init (Inventory.Start runs after Awake)
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= Refresh; // avoid double-sub
            Inventory.Instance.OnInventoryChanged += Refresh;
        }
        Refresh();
    }

    /// <summary>Updates all labels from the current Inventory state.</summary>
    public void Refresh()
    {
        if (Inventory.Instance == null) return;

        if (coinText   != null) coinText.text   = $"COIN: {Inventory.Instance.coins}";
        if (gemText    != null) gemText.text    = $"GEM: {Inventory.Instance.gems}";
        if (potionText != null) potionText.text = $"POT: {Inventory.Instance.potions}";
    }
}
