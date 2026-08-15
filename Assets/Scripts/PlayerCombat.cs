using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(InputReader))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform attackPoint;
    private CharacterController _cc;
    private InputReader _input;

    [Header("Combat Settings")]
    public float attackRange = 1.2f;
    public LayerMask enemyLayers;
    public int attackDamage = 10;
    
    [Header("Combo & Polish")]
    public float comboResetTime = 1.2f; 
    public float attackCooldown = 0.4f; 
    [Tooltip("How long a click is remembered before playing the next attack")]
    public float inputBufferWindow = 0.4f;
    [Tooltip("Slight forward push when swinging the sword")]
    public float attackForwardMomentum = 3f;

    // State
    public bool IsAttacking { get; private set; }
    public bool IsDefending { get; private set; }

    // Animator Hashes
    private static readonly int HashAttack1 = Animator.StringToHash("Attack01");
    private static readonly int HashAttack2 = Animator.StringToHash("Attack02");
    private static readonly int HashAttack3 = Animator.StringToHash("Attack03");
    private static readonly int HashAttack4 = Animator.StringToHash("Attack04");
    private static readonly int HashIsDefending = Animator.StringToHash("IsDefending");

    private int _comboStep = 0;
    private float _lastAttackTime;
    private bool _wasAttackPressed;
    
    // Input buffering
    private bool _bufferedAttack;
    private float _bufferTimer;

    // Momentum
    private float _currentMomentum;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _cc = GetComponent<CharacterController>();
        _input = GetComponent<InputReader>();
    }

    void Update()
    {
        HandleDefend();

        // Update attacking state based on cooldown
        IsAttacking = Time.time < _lastAttackTime + attackCooldown;

        // Only allow attacking if not defending
        if (animator != null && !animator.GetBool(HashIsDefending))
        {
            HandleAttack();
        }
        else
        {
            // Clear buffer if we started defending
            _bufferedAttack = false; 
        }

        ApplyMomentum();
    }

    void HandleAttack()
    {
        bool attackDown = _input != null && _input.IsAttacking;

        bool attackPressed = attackDown && !_wasAttackPressed;
        _wasAttackPressed = attackDown;

        // Reset combo if too much time has passed
        if (Time.time - _lastAttackTime > comboResetTime)
        {
            _comboStep = 0;
            _bufferedAttack = false;
        }

        // Buffer the input
        if (attackPressed)
        {
            _bufferedAttack = true;
            _bufferTimer = inputBufferWindow;
        }

        if (_bufferedAttack)
        {
            _bufferTimer -= Time.deltaTime;
            
            // If cooldown is finished and we have a buffered attack, execute it
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
                _bufferedAttack = false;
            }
            // If buffer ran out, forget the click
            else if (_bufferTimer <= 0f)
            {
                _bufferedAttack = false;
            }
        }
    }

    void PerformAttack()
    {
        if (animator == null) return;

        // Progress combo step
        _comboStep++;
        if (_comboStep > 4) _comboStep = 1;

        // Trigger corresponding animation
        switch (_comboStep)
        {
            case 1: animator.SetTrigger(HashAttack1); break;
            case 2: animator.SetTrigger(HashAttack2); break;
            case 3: animator.SetTrigger(HashAttack3); break;
            case 4: animator.SetTrigger(HashAttack4); break;
        }

        // Apply a burst of forward momentum
        _currentMomentum = attackForwardMomentum;

        // Hit detection
        DetectHits();
    }

    void ApplyMomentum()
    {
        if (_currentMomentum > 0.1f && _cc != null && _cc.enabled)
        {
            // Push player forward (ignoring Y)
            Vector3 pushDir = transform.forward;
            pushDir.y = 0;
            _cc.Move(pushDir * (_currentMomentum * Time.deltaTime));

            // Rapidly decay momentum
            _currentMomentum = Mathf.Lerp(_currentMomentum, 0f, 10f * Time.deltaTime);
        }
    }

    [Header("Hit Feedback")]
    [Tooltip("Optional particle spawned on each successful hit.")]
    public GameObject hitVFXPrefab;
    [Tooltip("Optional sound played on each successful hit.")]
    public AudioClip  hitSFX;

    public void DetectHits()
    {
        if (attackPoint == null) return;

        // If enemyLayers is 0 (unset in Inspector) fall back to ALL layers.
        // We then filter by EnemyHealth component, which is more reliable than layers.
        LayerMask mask = (enemyLayers.value == 0) ? ~0 : enemyLayers;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, mask);

        foreach (Collider enemy in hitEnemies)
        {
            // Skip the player's own colliders
            if (enemy.transform == transform || enemy.transform.IsChildOf(transform)) continue;

            // Find EnemyHealth on this object or any parent
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = enemy.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(attackDamage, transform);

                // VFX at hit point
                if (hitVFXPrefab != null)
                    Instantiate(hitVFXPrefab, enemy.ClosestPoint(attackPoint.position), Quaternion.identity);

                // SFX
                if (hitSFX != null)
                    AudioSource.PlayClipAtPoint(hitSFX, transform.position);

                Debug.Log($"[Combat] Hit {enemy.name} for {attackDamage} damage.");
            }
        }
    }

    void HandleDefend()
    {
        bool defendHeld = _input != null && _input.IsDefending; 

        IsDefending = defendHeld;

        if (animator != null)
        {
            animator.SetBool(HashIsDefending, defendHeld);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
