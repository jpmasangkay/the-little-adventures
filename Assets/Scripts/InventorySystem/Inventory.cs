using UnityEngine;

/// <summary>
/// Singleton inventory — one instance per scene, restored from GameManager between scenes.
/// Tracks coins, gems, and potions.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Current Counts (shown in Inspector at runtime)")]
    public int coins   = 0;
    public int gems    = 0;
    public int potions = 0;

    [Header("Capacities")]
    public int maxCoins = 9999;
    public int maxGems = 9999;
    public int maxPotions = 10;

    // Events so UI panels can react without polling
    public event System.Action OnInventoryChanged;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Restore from GameManager
        if (GameManager.Instance != null)
        {
            var (c, g, p)  = GameManager.Instance.LoadInventory();
            coins   = c;
            gems    = g;
            potions = p;
        }
        OnInventoryChanged?.Invoke();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void AddCoins(int amount)
    {
        coins   = Mathf.Clamp(coins + amount, 0, maxCoins);
        Notify();
    }

    public void AddGems(int amount)
    {
        gems    = Mathf.Clamp(gems + amount, 0, maxGems);
        Notify();
    }

    public void AddPotions(int amount)
    {
        potions = Mathf.Clamp(potions + amount, 0, maxPotions);
        Notify();
    }

    /// <summary>Consumes a potion if available. Returns true if consumed.</summary>
    public bool UsePotion()
    {
        if (potions <= 0) return false;
        potions--;
        Notify();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void Notify() => OnInventoryChanged?.Invoke();
}
