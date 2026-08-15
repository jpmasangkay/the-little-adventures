using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages player Experience Points (XP) and leveling up.
/// Syncs with GameManager on scene load.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Progression")]
    [Tooltip("How much the required XP increases per level")]
    public float xpMultiplierPerLevel = 1.5f;

    [Header("UI (Optional)")]
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public GameObject levelUpVFX;

    private void Start()
    {
        // Restore stats from GameManager if available
        if (GameManager.Instance != null)
        {
            var saved = GameManager.Instance.LoadStats();
            if (saved.level > 0)
            {
                currentLevel = saved.level;
                currentXP = saved.xp;
                CalculateNextLevelXP();
            }
        }
        
        UpdateUI();
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        UpdateUI();
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        CalculateNextLevelXP();

        // Optional: Heal player on level up
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(health.maxHealth);
        }

        if (levelUpVFX != null)
        {
            Instantiate(levelUpVFX, transform.position, Quaternion.identity, transform);
        }

        Debug.Log($"[PlayerStats] Leveled up to {currentLevel}!");
    }

    private void CalculateNextLevelXP()
    {
        // Simple scaling: 100, 150, 225, etc.
        xpToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(xpMultiplierPerLevel, currentLevel - 1));
    }

    private void UpdateUI()
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = xpToNextLevel;
            xpSlider.value = currentXP;
        }

        if (levelText != null)
        {
            levelText.text = $"Lvl {currentLevel}";
        }

        if (xpText != null)
        {
            xpText.text = $"{currentXP} / {xpToNextLevel} XP";
        }
    }
}
