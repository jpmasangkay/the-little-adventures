using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages HP for any enemy (Slime, TurtleShell, etc.).
/// Attach this alongside EnemyAI.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float currentHealth;
    [Tooltip("Amount of XP given to the player when this enemy dies.")]
    public int xpReward = 50;

    [Header("Death")]
    [Tooltip("Seconds to wait after the death animation before destroying the GameObject.")]
    public float deathDestroyDelay = 2f;
    [Tooltip("Optional particle effect to spawn on death.")]
    public GameObject deathVFXPrefab;
    [Tooltip("Optional loot prefab (coin, gem) to drop on death.")]
    public GameObject[] lootPrefabs;

    [Header("Hit Feedback")]
    [Tooltip("Optional particle to spawn on each hit.")]
    public GameObject hitVFXPrefab;
    [Tooltip("Seconds the enemy flashes red when hit.")]
    public float flashDuration = 0.2f;

    [Header("World Space Health Bar (optional)")]
    public Slider healthBarSlider;

    [Header("Animator Parameters")]
    [Tooltip("Trigger name for the hit reaction. Leave blank if your animator doesn't have one.")]
    public string hitTriggerName  = "GetHit";
    [Tooltip("Trigger name for the death animation. Leave blank if your animator doesn't have one.")]
    public string dieTriggerName  = "Die";

    // ── Internals ────────────────────────────────────────────────────────────
    private Animator   _animator;
    private Renderer[] _renderers;
    private Color[]    _originalColors;
    private bool       _isDead;

    // Validated animator hashes
    private int  _hashHit  = -1;
    private int  _hashDie  = -1;
    private bool _hasHit;
    private bool _hasDie;

    // URP uses _BaseColor; Built-in uses _Color — we detect which at Start
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropColor     = Shader.PropertyToID("_Color");

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _animator  = GetComponentInChildren<Animator>();
        _renderers = GetComponentsInChildren<Renderer>();

        // Cache original colours (works with both URP _BaseColor and Built-in _Color)
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material mat = _renderers[i].material;
            _originalColors[i] = mat.HasProperty(PropBaseColor)
                ? mat.GetColor(PropBaseColor)
                : mat.GetColor(PropColor);
        }

        // Validate animator parameters so we never spam "Hash does not exist" warnings
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in _animator.parameters)
            {
                if (!string.IsNullOrEmpty(hitTriggerName) && p.name == hitTriggerName)
                { _hashHit = p.nameHash; _hasHit = true; }
                if (!string.IsNullOrEmpty(dieTriggerName) && p.name == dieTriggerName)
                { _hashDie = p.nameHash; _hasDie = true; }
            }
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void TakeDamage(float amount, Transform attacker = null)
    {
        if (_isDead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthBar();

        // Floating damage number (yellow)
        DamagePopup.Create(transform.position, amount, isPlayerDamage: false);

        // Hit VFX
        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, transform.position + Vector3.up, Quaternion.identity);

        // Hit animation (only if parameter confirmed present)
        if (_animator != null && _hasHit)
            _animator.SetTrigger(_hashHit);

        StartCoroutine(FlashRed());

        // Stagger / Stun
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.Stun(0.5f);
        }

        // Knockback
        if (attacker != null)
        {
            Vector3 knockbackDir = (transform.position - attacker.position).normalized;
            knockbackDir.y = 0;
            StartCoroutine(ApplyKnockback(knockbackDir, 3f, 0.2f));
        }

        if (currentHealth <= 0f)
            Die();
    }

    private IEnumerator ApplyKnockback(Vector3 dir, float force, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Basic translation, assumes no rigidbody or complex physics
            transform.position += dir * (force * Time.deltaTime);
            yield return null;
        }
    }

    public bool IsDead => _isDead;

    // ── Private Helpers ──────────────────────────────────────────────────────

    private void Die()
    {
        _isDead = true;

        // Give XP to player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null) stats.AddXP(xpReward);
        }

        // Death animation
        if (_animator != null && _hasDie)
            _animator.SetTrigger(_hashDie);

        // Death VFX
        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Drop loot
        foreach (GameObject loot in lootPrefabs)
        {
            if (loot != null)
            {
                Vector3 spread = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
                Instantiate(loot, transform.position + spread, Quaternion.identity);
            }
        }

        // Disable AI and colliders
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Destroy(gameObject, deathDestroyDelay);
    }

    private IEnumerator FlashRed()
    {
        // Flash all renderers red (works with both URP and Built-in shader)
        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            Material mat = r.material;
            if (mat.HasProperty(PropBaseColor)) mat.SetColor(PropBaseColor, Color.red);
            else                                mat.SetColor(PropColor,     Color.red);
        }

        yield return new WaitForSeconds(flashDuration);

        // Restore original colours
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            Material mat = _renderers[i].material;
            if (mat.HasProperty(PropBaseColor)) mat.SetColor(PropBaseColor, _originalColors[i]);
            else                                mat.SetColor(PropColor,     _originalColors[i]);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value    = currentHealth;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
