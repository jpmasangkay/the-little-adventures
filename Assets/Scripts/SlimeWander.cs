using UnityEngine;

/// <summary>
/// Full enemy AI state machine for the Slime (and similar enemies).
///
/// States:
///   IDLE     — stand still for a moment before wandering
///   WANDER   — amble to a random nearby point within wanderRadius
///   CHASE    — pursue the player when they enter detectRange
///   ATTACK   — strike at a fixed rhythmic interval when within attackRange
///   COOLDOWN — very brief pause (0.3 s) after each attack
///
/// Rhythmic attacks:
///   The attack beat timer runs continuously — it is never reset on state
///   changes. This creates a predictable heartbeat the player can learn.
///
/// No NavMesh required — uses simple MoveTowards physics.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class SlimeWander : MonoBehaviour
{
    // ── States ────────────────────────────────────────────────────────────────
    private enum State { Idle, Wander, Chase, Attack, Cooldown }
    private State _state = State.Idle;

    // ── Wander Settings ───────────────────────────────────────────────────────
    [Header("Wander")]
    [Tooltip("Random point is chosen within this radius of the spawn position.")]
    public float wanderRadius  = 5f;
    [Tooltip("Movement speed while wandering.")]
    public float wanderSpeed   = 1f;
    [Tooltip("Minimum seconds to idle before picking a new wander target.")]
    public float wanderWaitMin = 1f;
    [Tooltip("Maximum seconds to idle before picking a new wander target.")]
    public float wanderWaitMax = 3f;

    // ── Detection ─────────────────────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Distance at which the enemy starts chasing the player.")]
    public float detectRange = 10f;

    // ── Chase Settings ────────────────────────────────────────────────────────
    [Header("Chase")]
    [Tooltip("Movement speed while chasing.")]
    public float chaseSpeed = 3f;

    // ── Attack Settings ───────────────────────────────────────────────────────
    [Header("Attack")]
    [Tooltip("Distance at which the enemy can hit the player.")]
    public float attackRange    = 2f;
    [Tooltip("Seconds between attacks — the rhythmic beat interval.")]
    public float attackInterval = 1.5f;
    [Tooltip("Damage dealt per hit.")]
    public float attackDamage   = 10f;
    [Tooltip("Slight knockback applied to the player on each hit.")]
    public float knockbackForce = 3f;
    [Tooltip("Brief pause after each attack before the next one.")]
    public float cooldownTime   = 0.3f;

    // ── Obstacle Avoidance ────────────────────────────────────────────────────
    [Header("Obstacle Avoidance")]
    [Tooltip("Which layers count as solid obstacles?")]
    public LayerMask obstacleMask;

    // ── References ────────────────────────────────────────────────────────────
    private EnemyHealth  _health;
    private Transform    _player;
    private PlayerHealth _playerHealth;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Vector3 _spawnPos;
    private Vector3 _wanderTarget;

    private float _idleTimer;          // counts down before next wander pick
    private float _obstacleTimer;      // throttles SphereCast to 5×/sec
    private float _cooldownTimer;      // post-attack pause
    private float _beatTimer;          // rhythmic attack beat — NEVER reset

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        _spawnPos = transform.position;

        // Default obstacle mask to Default layer if nothing is set
        if (obstacleMask == 0)
            obstacleMask = LayerMask.GetMask("Default");

        // Find player by tag then by component
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }
        if (playerObj != null)
        {
            _player       = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // Start with a short idle before the first wander
        EnterIdle();
    }

    private void Update()
    {
        if (_health != null && _health.IsDead) return;
        if (_player == null) return;

        // Tick the rhythmic beat every frame regardless of state
        _beatTimer -= Time.deltaTime;

        float distToPlayer = Vector3.Distance(transform.position, _player.position);

        // ── Global transition: player in range → chase / attack ───────────────
        bool playerNear = distToPlayer <= detectRange;

        switch (_state)
        {
            case State.Idle:
            case State.Wander:
                if (playerNear)
                {
                    _state = distToPlayer <= attackRange ? State.Attack : State.Chase;
                }
                break;
        }

        // ── Per-state behaviour ───────────────────────────────────────────────
        switch (_state)
        {
            case State.Idle:     DoIdle();     break;
            case State.Wander:   DoWander();   break;
            case State.Chase:    DoChase();    break;
            case State.Attack:   DoAttack();   break;
            case State.Cooldown: DoCooldown(); break;
        }
    }

    // ── IDLE ──────────────────────────────────────────────────────────────────
    private void DoIdle()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
        {
            PickWanderTarget();
            _state = State.Wander;
        }
    }

    // ── WANDER ────────────────────────────────────────────────────────────────
    private void DoWander()
    {
        Vector3 pos = transform.position;
        Vector3 dir = (_wanderTarget - pos).normalized;

        // Throttled obstacle check (5× per second)
        _obstacleTimer -= Time.deltaTime;
        if (_obstacleTimer <= 0f)
        {
            _obstacleTimer = 0.2f;
            if (Physics.SphereCast(pos + Vector3.up * 0.5f, 0.4f, dir, out _, wanderSpeed * 0.2f, obstacleMask))
            {
                // Hit something — stop and idle
                EnterIdle();
                return;
            }
        }

        // Move toward target
        transform.position = Vector3.MoveTowards(pos, _wanderTarget, wanderSpeed * Time.deltaTime);

        // Face movement direction
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        // Arrived?
        if ((transform.position - _wanderTarget).sqrMagnitude < 0.04f)
            EnterIdle();
    }

    // ── CHASE ─────────────────────────────────────────────────────────────────
    private void DoChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > detectRange)
        {
            // Player left range — return to wandering
            EnterIdle();
            return;
        }

        if (dist <= attackRange)
        {
            _state = State.Attack;
            return;
        }

        MoveToward(_player.position, chaseSpeed);
    }

    // ── ATTACK ────────────────────────────────────────────────────────────────
    private void DoAttack()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Player moved out of range?
        if (dist > attackRange)
        {
            _state = dist <= detectRange ? State.Chase : State.Idle;
            return;
        }

        // Always face the player while attacking
        FaceTarget(_player.position);

        // Fire on the beat
        if (_beatTimer <= 0f)
        {
            _beatTimer = attackInterval;

            // Deal damage
            if (_playerHealth != null && !_playerHealth.IsDead)
            {
                _playerHealth.TakeDamage(attackDamage);

                // Knockback
                var cc = _player.GetComponent<CharacterController>();
                if (cc != null && cc.enabled)
                {
                    Vector3 kb = (_player.position - transform.position).normalized * knockbackForce;
                    kb.y = 0.5f;
                    cc.Move(kb * Time.deltaTime * 10f);
                }
            }

            // Brief cooldown pause after hit
            _cooldownTimer = cooldownTime;
            _state = State.Cooldown;
        }
    }

    // ── COOLDOWN ──────────────────────────────────────────────────────────────
    private void DoCooldown()
    {
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            _state = dist <= attackRange ? State.Attack
                   : dist <= detectRange ? State.Chase
                   : State.Idle;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void EnterIdle()
    {
        _idleTimer = Random.Range(wanderWaitMin, wanderWaitMax);
        _state = State.Idle;
    }

    private void PickWanderTarget()
    {
        Vector2 circle = Random.insideUnitCircle * wanderRadius;
        _wanderTarget  = _spawnPos + new Vector3(circle.x, 0f, circle.y);
    }

    private void MoveToward(Vector3 target, float speed)
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
            transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Wander radius — yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? _spawnPos : transform.position, wanderRadius);

        // Detect range — blue
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Attack range — red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
