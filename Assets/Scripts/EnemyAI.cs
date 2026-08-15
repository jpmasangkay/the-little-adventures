using UnityEngine;

/// <summary>
/// Two-state AI for Slime and TurtleShell enemies:
///   Idle  — enemy stands still, scanning for the player.
///   Aggro — enemy has line-of-sight to the player; chases and attacks.
///
/// The enemy will NOT aggro through walls — it requires a clear raycast
/// to the player's position every frame to remain aggressive.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    // ── States ────────────────────────────────────────────────────────────────
    private enum State { Idle, Aggro, Attacking, Stunned }
    private State _state = State.Idle;

    // ── Aggro Settings ────────────────────────────────────────────────────────
    [Header("Aggro")]
    [Tooltip("Distance at which the enemy can SEE the player (requires clear line of sight).")]
    public float sightRange   = 8f;
    [Tooltip("Height offset on the enemy to use as the 'eye' position for raycasts.")]
    public float eyeHeight    = 0.8f;
    [Tooltip("Layers that block line of sight (walls, terrain, etc).")]
    public LayerMask sightBlockMask;
    [Tooltip("If the player breaks line of sight, how long before the enemy gives up and returns to Idle.")]
    public float losGraceTime = 2f;

    // ── Chase Settings ────────────────────────────────────────────────────────
    [Header("Chase")]
    public float chaseSpeed = 2.5f;

    // ── Attack Settings ───────────────────────────────────────────────────────
    [Header("Attack")]
    [Tooltip("Distance at which the enemy can hit the player.")]
    public float attackRange    = 1.5f;
    public float attackDamage   = 10f;
    [Tooltip("Seconds between individual attacks.")]
    public float attackCooldown = 1.5f;
    [Tooltip("Slight knockback force applied to the player on hit.")]
    public float knockbackForce = 3f;

    // ── Animator Parameters ───────────────────────────────────────────────────
    [Header("Animator Parameters")]
    [Tooltip("Float parameter name for movement speed. Leave blank if unused.")]
    public string speedParamName  = "Speed";
    [Tooltip("Trigger parameter name for the attack animation. Leave blank if unused.")]
    public string attackParamName = "Attack";

    // ── References ────────────────────────────────────────────────────────────
    private Animator     _animator;
    private EnemyHealth  _health;
    private Transform    _player;
    private PlayerHealth _playerHealth;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float _lastAttackTime;
    private float _losLostTimer;   // counts down while player is out of sight
    private float _stunTimer;
    private float _attackTimer;

    // Validated animator hashes (set in Awake)
    private int  _hashSpeed  = -1;
    private int  _hashAttack = -1;
    private bool _hasSpeed;
    private bool _hasAttack;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _health   = GetComponent<EnemyHealth>();

        // Default sight block mask: everything except the enemy itself
        if (sightBlockMask == 0)
            sightBlockMask = ~(1 << gameObject.layer);

        // Validate animator parameters (avoids "Hash does not exist" spam)
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in _animator.parameters)
            {
                if (!string.IsNullOrEmpty(speedParamName)  && p.name == speedParamName)
                { _hashSpeed = p.nameHash; _hasSpeed = true; }
                if (!string.IsNullOrEmpty(attackParamName) && p.name == attackParamName)
                { _hashAttack = p.nameHash; _hasAttack = true; }
            }
        }
    }

    private void Start()
    {
        // Find player — tag first, then component scan
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }

        if (playerObj != null)
        {
            _player       = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        SetAnimatorSpeed(0f); // Start in idle pose
    }

    private void Update()
    {
        if (_health.IsDead) return;
        if (_player == null) return;

        bool canSeePlayer = HasLineOfSight();

        // ── State Transitions ─────────────────────────────────────────────────
        switch (_state)
        {
            case State.Stunned:
                _stunTimer -= Time.deltaTime;
                if (_stunTimer <= 0f)
                    _state = canSeePlayer ? State.Aggro : State.Idle;
                break;
                
            case State.Attacking:
                // Do not transition out normally, let DoAttacking handle it
                break;

            case State.Idle:
                if (canSeePlayer)
                {
                    _state = State.Aggro;
                    _losLostTimer = losGraceTime;
                }
                break;

            case State.Aggro:
                if (canSeePlayer)
                {
                    _losLostTimer = losGraceTime; // reset grace timer while visible
                }
                else
                {
                    _losLostTimer -= Time.deltaTime;
                    if (_losLostTimer <= 0f)
                        _state = State.Idle;       // gave up — player hid
                }
                break;
        }

        // ── Behaviour per State ───────────────────────────────────────────────
        switch (_state)
        {
            case State.Idle:      DoIdle();      break;
            case State.Aggro:     DoAggro();     break;
            case State.Attacking: DoAttacking(); break;
            case State.Stunned:   DoStunned();   break;
        }
    }

    // ── Line-of-Sight ─────────────────────────────────────────────────────────
    private bool HasLineOfSight()
    {
        if (_player == null) return false;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > sightRange) return false; // too far — skip raycast

        Vector3 eye    = transform.position + Vector3.up * eyeHeight;
        Vector3 target = _player.position   + Vector3.up * 0.8f; // aim at player chest
        Vector3 dir    = target - eye;

        // If nothing blocks the ray between eye and player → clear sight
        return !Physics.Raycast(eye, dir.normalized, dir.magnitude, sightBlockMask,
                                 QueryTriggerInteraction.Ignore);
    }

    // ── Idle ──────────────────────────────────────────────────────────────────
    private void DoIdle()
    {
        SetAnimatorSpeed(0f);
        // Enemy just stands still — no wandering
    }

    // ── Aggro ─────────────────────────────────────────────────────────────────
    private void DoAggro()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange)
        {
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                // In attack range and cooldown ready — start attack
                _state = State.Attacking;
                _attackTimer = 0.5f; // Wind-up duration
                SetAnimatorSpeed(0f);
                FaceTarget(_player.position);
                
                if (_animator != null && _hasAttack)
                    _animator.SetTrigger(_hashAttack);
            }
            else
            {
                // Waiting for cooldown, just stare at player
                SetAnimatorSpeed(0f);
                FaceTarget(_player.position);
            }
        }
        else
        {
            // Chase the player
            SetAnimatorSpeed(chaseSpeed);
            MoveTowards(_player.position, chaseSpeed);
        }
    }

    // ── Attack ────────────────────────────────────────────────────────────────
    private void DoAttacking()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            // Apply damage if still in range
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= attackRange + 0.2f) // Give a tiny bit of leniency
            {
                if (_playerHealth != null && !_playerHealth.IsDead)
                {
                    _playerHealth.TakeDamage(attackDamage, transform); // Pass our transform!

                    CharacterController cc = _player.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled)
                    {
                        Vector3 kb = (_player.position - transform.position).normalized * knockbackForce;
                        kb.y = 0.5f;
                        cc.Move(kb * Time.deltaTime * 10f);
                    }
                }
            }
            
            _lastAttackTime = Time.time;
            _state = State.Aggro; // Back to aggro
        }
    }

    // ── Stun ──────────────────────────────────────────────────────────────────
    private void DoStunned()
    {
        SetAnimatorSpeed(0f);
    }
    
    public void Stun(float duration)
    {
        _state = State.Stunned;
        _stunTimer = duration;
    }

    // ── Movement Helpers ──────────────────────────────────────────────────────
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(target.x, transform.position.y, target.z),
            speed * Time.deltaTime);

        FaceTarget(target);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            8f * Time.deltaTime);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (_animator != null && _hasSpeed)
            _animator.SetFloat(_hashSpeed, speed, 0.1f, Time.deltaTime);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Sight range — blue
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Attack range — red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Line-of-sight ray (editor only — shows in Scene view)
        if (_player != null)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.grey;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight,
                            _player.position   + Vector3.up * 0.8f);
        }
    }
}
