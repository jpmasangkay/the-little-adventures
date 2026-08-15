using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Tracks the player's health, syncs with GameManager on scene load, and triggers
/// a respawn (reloading the respawn scene) when the player dies.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI References")]
    public UnityEngine.UI.Slider healthSlider;
    public UnityEngine.UI.Image  healthFill;

    [Header("Respawn")]
    [Tooltip("How long to wait (e.g. for death animation) before loading the respawn scene.")]
    public float respawnDelay   = 2f;
    [Tooltip("Optional: full-screen fade-out image that goes black before respawning.")]
    public UnityEngine.UI.Image screenFade;
    [Tooltip("Optional: full-screen red image that flashes briefly when the player takes damage.")]
    public UnityEngine.UI.Image damageFlashImage;

    // Public flag so EnemyAI can check before applying damage
    public bool IsDead { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Restore health from GameManager if available
        if (GameManager.Instance != null)
        {
            float saved = GameManager.Instance.LoadHealth();
            currentHealth = (saved > 0f) ? saved : maxHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        // ── Auto-find health UI if not wired in the Inspector ──────────────
        if (healthSlider == null && healthFill == null)
        {
            // Tier 1: name-based search (catches "Healthbar_Custom", "HealthBar", "HP_Slider" etc.)
            var allSliders = Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include);
            foreach (var s in allSliders)
            {
                string n = s.gameObject.name.ToLower();
                if (n.Contains("health") || n.Contains("hp") || n.Contains("healthbar"))
                {
                    healthSlider = s;
                    Debug.Log($"[PlayerHealth] Auto-found Slider by name: '{s.gameObject.name}'");
                    break;
                }
            }

            // Tier 2: search inside HUDCanvas
            if (healthSlider == null)
            {
                GameObject hud = GameObject.Find("HUDCanvas");
                if (hud != null)
                {
                    var slider = hud.GetComponentInChildren<UnityEngine.UI.Slider>(includeInactive: true);
                    if (slider != null)
                    {
                        healthSlider = slider;
                        Debug.Log($"[PlayerHealth] Auto-found Slider in HUDCanvas: '{slider.gameObject.name}'");
                    }
                }
            }

            // Tier 3: just use the first slider in the scene
            if (healthSlider == null && allSliders.Length > 0)
            {
                healthSlider = allSliders[0];
                Debug.Log($"[PlayerHealth] Auto-found first Slider in scene: '{allSliders[0].gameObject.name}'");
            }
        }

        if (healthSlider == null && healthFill == null)
            Debug.LogWarning("[PlayerHealth] No health UI found. Drag a Slider into the Health Slider field.");

        IsDead = false;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (IsDead) return;

        InputReader input = GetComponent<InputReader>();
        if (input != null && input.PotionPressedThisFrame)
        {
            TryUsePotion();
        }
    }

    public void TryUsePotion()
    {
        if (IsDead) return;
        
        if (Inventory.Instance != null && Inventory.Instance.UsePotion())
        {
            Heal(30f);
            // Optional: add a healing particle effect or sound here
            Debug.Log("[PlayerHealth] Potion used! Healed 30 HP.");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void TakeDamage(float amount, Transform attacker = null)
    {
        if (IsDead) return;

        // --- Shield Block Logic ---
        if (attacker != null)
        {
            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.IsDefending)
            {
                // Check if the attacker is in front of the player
                Vector3 dirToAttacker = (attacker.position - transform.position).normalized;
                dirToAttacker.y = 0f;
                Vector3 playerForward = transform.forward;
                playerForward.y = 0f;

                float dot = Vector3.Dot(playerForward.normalized, dirToAttacker.normalized);
                
                // If dot > 0, the attacker is in the front 180 degrees. 
                // We'll use 0.2f for a slightly tighter 140 degree block angle.
                if (dot > 0.2f)
                {
                    Debug.Log($"[PlayerHealth] Blocked attack from {attacker.name}!");
                    
                    // Optional: Play a block sound or spark particle here if available
                    // AudioSource.PlayClipAtPoint(blockSound, transform.position);

                    return; // Damage completely negated
                }
                else
                {
                    Debug.Log($"[PlayerHealth] Hit from behind! Shield bypassed.");
                }
            }
        }
        // --------------------------

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthBar();

        // Floating red damage number above the player
        DamagePopup.Create(transform.position, amount, isPlayerDamage: true);

        // Red screen flash
        if (damageFlashImage != null)
            StartCoroutine(FlashScreen());

        if (currentHealth <= 0f)
            StartCoroutine(Die());
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth += amount;
        currentHealth  = Mathf.Min(maxHealth, currentHealth);
        UpdateHealthBar();
    }

    private IEnumerator FlashScreen()
    {
        if (damageFlashImage == null) yield break;
        damageFlashImage.gameObject.SetActive(true);
        Color c = new Color(1f, 0f, 0f, 0.45f);
        damageFlashImage.color = c;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.45f, 0f, t / 0.35f);
            damageFlashImage.color = c;
            yield return null;
        }
        damageFlashImage.gameObject.SetActive(false);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator Die()
    {
        IsDead = true;
        Debug.Log("[PlayerHealth] Player died — respawning...");

        // Tell the GameManager the player is dead (reset saved health)
        if (GameManager.Instance != null)
            GameManager.Instance.savedHealth = -1f; // -1 → "use maxHealth on next Start"

        // Optional: fade to black
        if (screenFade != null)
        {
            float t = 0f;
            Color c = screenFade.color;
            c.a = 0f;
            screenFade.gameObject.SetActive(true);
            while (t < respawnDelay)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / respawnDelay);
                screenFade.color = c;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(respawnDelay);
        }

        // Load the respawn scene
        string scene = (GameManager.Instance != null)
            ? GameManager.Instance.respawnSceneName
            : "Village";
        SceneManager.LoadScene(scene);
    }

    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value    = currentHealth;
        }
        else if (healthFill != null)
        {
            healthFill.fillAmount = currentHealth / maxHealth;
        }
    }
}
