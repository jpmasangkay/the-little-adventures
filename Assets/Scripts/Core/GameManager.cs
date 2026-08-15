using UnityEngine;

/// <summary>
/// A persistent singleton that carries game state (player health, inventory) across scene loads.
/// Place one instance in your first scene (e.g. Village). It will survive every subsequent load.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Persisted Player State ───────────────────────────────────────────────
    [Header("Persisted Player State")]
    [Tooltip("Player health carried between scenes. -1 means 'use PlayerHealth.maxHealth' on first load.")]
    public float savedHealth = -1f;

    [Header("Persisted Inventory")]
    public int savedCoins   = 0;
    public int savedGems    = 0;
    public int savedPotions = 0;

    [Header("Persisted Stats")]
    public int savedLevel = -1;
    public int savedXP = 0;

    // ── Spawn Point ──────────────────────────────────────────────────────────
    /// <summary>
    /// The scene name the player should respawn in after dying.
    /// </summary>
    public string respawnSceneName = "Village";

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Save / Load helpers (called by ScenePortal & PlayerHealth) ───────────

    /// <summary>
    /// Called by ScenePortal before loading a new scene to snapshot the player's current state.
    /// </summary>
    public void SavePlayerState(PlayerHealth health, Inventory inventory, PlayerStats stats = null)
    {
        if (health != null)
            savedHealth = health.currentHealth;

        if (inventory != null)
        {
            savedCoins   = inventory.coins;
            savedGems    = inventory.gems;
            savedPotions = inventory.potions;
        }

        if (stats != null)
        {
            savedLevel = stats.currentLevel;
            savedXP = stats.currentXP;
        }
    }

    /// <summary>
    /// Called by PlayerHealth.Start() to restore the last-saved health value.
    /// Returns the saved health, or -1 if this is the very first load.
    /// </summary>
    public float LoadHealth() => savedHealth;

    /// <summary>
    /// Called by Inventory.Start() to restore the last-saved inventory.
    /// </summary>
    public (int coins, int gems, int potions) LoadInventory()
        => (savedCoins, savedGems, savedPotions);

    /// <summary>
    /// Called by PlayerStats.Start() to restore the last-saved stats.
    /// </summary>
    public (int level, int xp) LoadStats()
        => (savedLevel, savedXP);
}
